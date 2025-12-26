using Microsoft.Extensions.Options;
using Models.Dto.V1.Requests;
using WebAPI.BLL.Models;
using WebAPI.Clients;
using WebAPI.Config;
using WebAPI.Consumer.Base;
using static Models.Dto.V1.Requests.V1CreateOrderRequest;

namespace WebAPI.Consumer.Consumers
{
    public class BatchOmsOrderCreatedConsumer(
    IOptions<RabbitMqSettings> rabbitMqSettings,
    IServiceProvider serviceProvider)
    : BaseBatchMessageConsumer<OrderUnit>(rabbitMqSettings.Value, s => s.OrderCreated)
    {
        private static int globalBatchCount = 0;
        protected override async Task ProcessMessages(OrderUnit[] messages)
        {
            
            using var scope = serviceProvider.CreateScope();
            
            var client = scope.ServiceProvider.GetRequiredService<OmsClient>();
            try
            {

                if (messages == null || messages.Length == 0)
                {
                    //Console.WriteLine("DEBUG: Массив messages пуст или null");
                    return;
                }

                var ordersList = new List<V1AuditLogOrderRequest.LogOrder>();

                foreach (var m in messages)
                {
                    if (m.OrderItems == null)
                    {
                        //Console.WriteLine($"DEBUG: У заказа {m.Id} OrderItems == null");
                        continue;
                    }

                    foreach (var item in m.OrderItems)
                    {
                        ordersList.Add(new V1AuditLogOrderRequest.LogOrder
                        {
                            OrderId = m.Id,
                            OrderItemId = item.Id,
                            CustomerId = m.CustomerId,
                            OrderStatus = "Created",
                            
                        });
                    }
                }

                if (ordersList.Count > 0)
                {

                    int currentBatch = Interlocked.Increment(ref globalBatchCount);
                    if (currentBatch % 5 == 0)
                    {
                        throw new Exception("FFFF!!!!");

                    }
                    await client.LogOrder
                        (new V1AuditLogOrderRequest
                        {
                            Orders = ordersList.ToArray(),
                        },
                        CancellationToken.None);
                }
                else
                {
                    throw new Exception("Пустое сообщение");
                }

            }
            catch (Exception ex)
            {
                //Console.WriteLine("Err Created "+ex.ToString());
                throw;
            }
        }
    }
}
