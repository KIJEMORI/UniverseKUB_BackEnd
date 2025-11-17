using WebAPI.BLL.Models;
using WebAPI.DAL.Interfaces;
using WebAPI.DAL.Models;
using WebAPI.DAL;
using static Models.Dto.V1.Requests.V1CreateOrderRequest;
using static Dapper.SqlMapper;
using System.Threading;
using Microsoft.Extensions.Options;
using WebAPI.Config;

using Newtonsoft.Json.Linq;
using System.Transactions;
using Microsoft.AspNetCore.Http.HttpResults;

namespace WebAPI.BLL.Services
{
    public class OrderService(UnitOfWork unitOfWork, IOrderRepository orderRepository, IOrderItemRepository orderItemRepository, IAuditLogRepository auditLogRepository, RabbitMqService _rabbitMqService, IOptions<RabbitMqSettings> settings)
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

                    

                    List<V1AuditLogDal> listV1AuditLogDal = new List<V1AuditLogDal>();
                    foreach (var orderItem in order.OrderItems)
                    {
                        orderItem.Id = insert_2[index_2].Id;

                        listV1AuditLogDal.Add(new V1AuditLogDal
                        {
                            OrderId = order.Id,
                            OrderItemId = orderItem.Id,
                            CustomerId = order.CustomerId,
                            OrderStatus = "Created",
                            CreatedAt = now,
                            UpdatedAt = now
                        });

                        index_2++;
                    }

                    var inser_audit_log = await auditLogRepository.BulkInsert(listV1AuditLogDal.ToArray(), token);
                }
                await transaction.CommitAsync(token);

                messagesToPublish = orderUnits;

                var messages = orderUnits;
                await _rabbitMqService.Publish(messages, settings.Value.OrderCreatedQueue, token);



                return orderUnits;
            }
            catch (Exception e)
            {
                if(transaction.Connection != null){
                    await transaction.RollbackAsync(token);
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
            List<V1AuditLogDal> v1AuditLogDals = new List<V1AuditLogDal>();
            foreach (var x in model) {
                V1AuditLogDal[] logs = await
                auditLogRepository.Query(
                    new QueryAuditLogDalModel
                    {
                        OrderId = x.OrderId,
                        OrderItemId = x.OrderItemId,
                        CustomerId = x.CustomerId,
                        OrderStatus = x.OrderStatus
                    }
                    , token
                );

                foreach (var log in logs)
                {
                    v1AuditLogDals.Add(log);
                }
            }

            var searched = v1AuditLogDals.ToArray();

            var res = new List<OrderUnit>();

            foreach (var x in searched) {
                var y = await GetOrders(new QueryOrderItemsModel
                {
                    Ids = [x.OrderId],
                    CustomerIds = [x.CustomerId],
                    IncludeOrderItems = true
                }, token);
                foreach (var log in y)
                {
                    res.Add(log);
                }
            }

            return res.ToArray();

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
