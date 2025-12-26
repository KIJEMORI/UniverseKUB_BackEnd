
using WebAPI.DAL.Interfaces;
using WebAPI.DAL.Models;
using WebAPI.DAL;
using static Models.Dto.V1.Requests.V1CreateOrderRequest;
using static Dapper.SqlMapper;
using System.Threading;
using Microsoft.Extensions.Options;
using WebAPI.Config;
using WebAPI.Base;
using WebAPI.BLL.Models;
using Models.Dto.Common;
using OrderUnit = WebAPI.BLL.Models.OrderUnit;
using OrderItemUnit = WebAPI.BLL.Models.OrderItemUnit;


namespace WebAPI.BLL.Services
{
    public class OrderService(UnitOfWork unitOfWork, IOrderRepository orderRepository, IOrderItemRepository orderItemRepository, IAuditLogRepository auditLogRepository, KafkaProducer kafkaProducer, IOptions<KafkaSettings> settings)
    {
        /// <summary>
        /// Метод создания заказов
        /// </summary>
        public async Task<OrderUnit[]> BatchInsert(OrderUnit[] orderUnits, CancellationToken token)
        { 
            var now = DateTimeOffset.UtcNow;
            await using var transaction = await unitOfWork.BeginTransactionAsync(token);

            OrderUnit[] messagesToPublish = null;

            try
            {

                List<(string, OrderUnit)> listMessages = new List<(string, OrderUnit)>();
                // тут ваш бизнес код по инсерту данных в БД
                // нужно положить в БД заказы(orders), а потом их позиции (orderItems)
                // помните, что каждый orderItem содержит ссылку на order (столбец order_id)
                // OrderItem-ов может быть несколько
                V1OrderDal[] v1OrdersDal = new V1OrderDal[orderUnits.Length];
                int index = 0;
                foreach (var order in orderUnits) {
                    
                    var v1Order = new V1OrderDal
                    {
                        Id = order.Id,
                        CustomerId = order.CustomerId,
                        DeliveryAddress = order.DeliveryAddress,
                        TotalPriceCents = order.TotalPriceCents,
                        TotalPriceCurrency = order.TotalPriceCurrency,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    listMessages.Add((order.CustomerId.ToString(), order));

                    order.CreatedAt = now;
                    order.UpdatedAt = now;
                    v1OrdersDal[index] = v1Order;
                    
                    index++;
                    
                }
                var insert = await orderRepository.BulkInsert(v1OrdersDal, token);

                index = 0;
                foreach (var order in orderUnits)
                {

                    
                    order.Id = insert[index].Id;
                    List<V1OrderItemDal> listV1OrderItemsDal = new List<V1OrderItemDal>();
                    foreach (var orderItem in order.OrderItems)
                    {
                        var v1OrderItem = new V1OrderItemDal
                        {
                            Id = orderItem.Id,
                            OrderId = insert[index].Id,
                            ProductId = orderItem.ProductId,
                            Quantity = orderItem.Quantity,
                            ProductTitle = orderItem.ProductTitle,
                            ProductUrl = orderItem.ProductUrl,
                            PriceCents = orderItem.PriceCents,
                            PriceCurrency = orderItem.PriceCurrency,
                            CreatedAt = now,
                            UpdatedAt = now
                        };
                        orderItem.OrderId = insert[index].Id;
                        orderItem.CreatedAt = now;
                        orderItem.UpdatedAt = now;
                        
                        listV1OrderItemsDal.Add(v1OrderItem);
                    }
                    index++;
                    var insert_2 = await orderItemRepository.BulkInsert(listV1OrderItemsDal.ToArray(), token);
                    var index_2 = 0;

                    

                }
                await transaction.CommitAsync(token);

                messagesToPublish = orderUnits;

                var messages = orderUnits;

                var message = new OmsOrderCreatedMessage[messages.Length];

                for(int i =0; i < messages.Length; i++)
                {
                    message[i] = new OmsOrderCreatedMessage
                    {
                        Message = messages[i],
                    };
                }


                await kafkaProducer.Produce(settings.Value.OmsOrderCreatedTopic, listMessages.ToArray(), token);


                return orderUnits;
            }
            catch (Exception e)
            {
                try
                {
                   await transaction.RollbackAsync(token);
                }
                catch (InvalidOperationException)
                {
                   
                }
                catch (Exception rollbackEx)
                {
                    
                }

                throw;
            }

            
        }

        /// <summary>
        /// Метод получения заказов
        /// </summary>
        public async Task<OrderUnit[]> GetOrders(QueryOrderItemsModel model, CancellationToken token)
        {
            var orders = await orderRepository.Query(new QueryOrdersDalModel
            {
                Ids = model.Ids,
                CustomerIds = model.CustomerIds,
                Limit = model.PageSize,
                Offset = (model.Page - 1) * model.PageSize
            }, token);

            if (orders.Length is 0)
            {
                return [];
            }

            ILookup<long, V1OrderItemDal> orderItemLookup = null;
            if (model.IncludeOrderItems)
            {
                var orderItems = await orderItemRepository.Query(new QueryOrderItemsDalModel
                {
                    OrderIds = orders.Select(x => x.Id).ToArray(),
                }, token);

                orderItemLookup = orderItems.ToLookup(x => x.OrderId);
            }

            return Map(orders, orderItemLookup);
        }

        public async Task<OrderUnit[]> LogOrder(AuditLogUnit[] model, CancellationToken token)
        {

            await using var transaction = await unitOfWork.BeginTransactionAsync(token);

            /*V1AuditLogDal[] searched = await auditLogRepository.Query(
                new QueryAuditLogDalModel
                {
                    OrderIds = orderIds, // Передаем МАССИВ ID
                    CustomerIds = customerIds, // Передаем МАССИВ ID
                                               // OrderId/CustomerId/OrderStatus = null, // Убираем фильтры по одиночным значениям
                },
                token
            );

            var res = await GetOrders(new QueryOrderItemsModel
            {
                Ids = orderIds, // Передаем МАССИВ ID
                CustomerIds = customerIds, // Передаем МАССИВ ID
                IncludeOrderItems = true
            }, token);

            return res.ToArray();
*/

            try
            {
                var time = DateTimeOffset.UtcNow;
                var listV1AuditLogDal = model.Select(mod => new V1AuditLogDal
                {
                    OrderId = mod.OrderId, // Проверьте: в вашем коде было mod.Id, должно быть OrderId?
                    OrderItemId = mod.OrderItemId,
                    CustomerId = mod.CustomerId,
                    OrderStatus = mod.OrderStatus,
                    CreatedAt = time,
                    UpdatedAt = time
                }).ToArray();

                var res = await auditLogRepository.BulkInsert(listV1AuditLogDal, token);

                var orderIds = model.Select(x => x.OrderId).Distinct().ToArray();
                var customerIds = model.Select(x => x.CustomerId).Distinct().ToArray();

                var res_2 = await GetOrders(new QueryOrderItemsModel
                {
                    Ids = orderIds, // Передаем МАССИВ ID
                    CustomerIds = customerIds, // Передаем МАССИВ ID
                    IncludeOrderItems = true
                }, token);

                await transaction.CommitAsync(token);

                return res_2.ToArray();
            }
            catch (Exception ex)
            {
                if (transaction.Connection != null)
                {
                    await transaction.RollbackAsync(token);
                }
                throw;
            }



        }

        public async Task<OrderUnit[]> UpdateOrderStatus(UpdateOrderStatus[] model, CancellationToken token)
        {
            await using var transaction = await unitOfWork.BeginTransactionAsync(token);
            try
            {

                List<V1OrderDal> messagesList = new List<V1OrderDal>();
                (string, UpdateOrderStatus)[] listMessages = model.Select(x => ((string)null, x)).ToArray();

                foreach (var req in model) {

                    var res = await orderRepository.Update(
                        new UpdateOrderDalModel
                        {
                            Ids = req.OrderIds,
                            NewStatus = req.Status
                        },
                        token
                    );
                    messagesList.AddRange(res);
                }


               

                var messages = messagesList.ToArray();


                var message = new OmsOrderStatusChangedMessage[model.Length];

                for (int i = 0; i < model.Length; i++)
                {
                    message[i] = new OmsOrderStatusChangedMessage
                    {
                        Message = model[i]
                    };
                }

                await transaction.CommitAsync(token);

                await kafkaProducer.Produce(settings.Value.OmsOrderStatusChangedTopic, listMessages, token);

                

                return Map(messages);
            }
            catch (Exception ex)
            {
                if(transaction.Connection != null){
                    await transaction.RollbackAsync(token);
                }
                throw;
            }


        }



        private OrderUnit[] Map(V1OrderDal[] orders, ILookup<long, V1OrderItemDal> orderItemLookup = null)
        {
            return orders.Select(x => new OrderUnit
            {
                Id = x.Id,
                CustomerId = x.CustomerId,
                DeliveryAddress = x.DeliveryAddress,
                TotalPriceCents = x.TotalPriceCents,
                TotalPriceCurrency = x.TotalPriceCurrency,
                Status = x.Status,
                CreatedAt = x.CreatedAt,
                UpdatedAt = x.UpdatedAt,
                OrderItems = orderItemLookup?[x.Id].Select(o => new OrderItemUnit
                {
                    Id = o.Id,
                    OrderId = o.OrderId,
                    ProductId = o.ProductId,
                    Quantity = o.Quantity,
                    ProductTitle = o.ProductTitle,
                    ProductUrl = o.ProductUrl,
                    PriceCents = o.PriceCents,
                    PriceCurrency = o.PriceCurrency,
                    CreatedAt = o.CreatedAt,
                    UpdatedAt = o.UpdatedAt
                }).ToArray() ?? []
            }).ToArray();
        }
    }
}
