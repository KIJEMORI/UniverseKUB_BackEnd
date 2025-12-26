using Microsoft.Extensions.Options;
using Models.Dto.V1.Requests;
using WebAPI.BLL.Models;
using WebAPI.Clients;
using WebAPI.Config;
using WebAPI.Consumer.Base;
using WebAPI.DAL.Models;
using static Models.Dto.V1.Requests.V1CreateOrderRequest;

namespace WebAPI.Consumer.Consumers
{
    public class BatchOmsOrderStatusChangedConsumer(
    IOptions<RabbitMqSettings> rabbitMqSettings,
    IServiceProvider serviceProvider)
    : BaseBatchMessageConsumer<UpdateOrderStatus>(rabbitMqSettings.Value, s => s.OrderStatusChanged)
    {
        private static int globalBatchCount = 0;
        protected override async Task ProcessMessages(UpdateOrderStatus[] messages)
        {

            if (messages == null || messages.Length == 0)
            {
                return;
            }

            using var scope = serviceProvider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

            try
            {
 
                var ordersList = new List<V1AuditLogOrderRequest.LogOrder>();

                foreach (var message in messages)
                {
                    if (message?.OrderIds == null) continue;

                    foreach (var id in message.OrderIds)
                    {
                        ordersList.Add(new V1AuditLogOrderRequest.LogOrder
                        {
                            OrderId = id,
                            OrderStatus = message.Status
                        });
                    }
                }

                

                if (ordersList.Count > 0)
                {
                    Console.WriteLine(ordersList[0]);

                    int currentBatch = Interlocked.Increment(ref globalBatchCount);
                    if (currentBatch % 5 == 0)
                    {
                        throw new Exception($"Искусственная ошибка!");
                    }

                    var request = new V1AuditLogOrderRequest
                    {
                        Orders = ordersList.ToArray()
                    };

                    await client.LogOrder(request, CancellationToken.None);
                }
                else
                {
                    throw new InvalidOperationException("В полученных сообщениях отсутствуют OrderIds");
                }
            }
            catch (Exception ex)
            {
                // logger.LogError(ex, "Ошибка при обновлении статусов заказов");
                throw;
            }

        }
    }
}
