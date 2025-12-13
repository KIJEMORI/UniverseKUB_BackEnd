using Microsoft.Extensions.Options;
using Models.Dto.V1.Requests;
using WebAPI.BLL.Models;
using WebAPI.Clients;
using WebAPI.Config;
using WebAPI.Consumer.Base;
using WebAPI.DAL.Models;

namespace WebAPI.Consumer.Consumers
{
    public class BatchOmsOrderStatusChangedConsumer(
    IOptions<RabbitMqSettings> rabbitMqSettings,
    IServiceProvider serviceProvider)
    : BaseBatchMessageConsumer<UpdateOrderStatus>(rabbitMqSettings.Value, s => s.OrderStatusChanged)
    {
        protected override async Task ProcessMessages(UpdateOrderStatus[] messages)
        {

            using var scope = serviceProvider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

            try
            { 
                await client.LogOrder(new V1AuditLogOrderRequest
                {
                    Orders = messages.Select(order => // Используем chunk вместо messages
                        new V1AuditLogOrderRequest.LogOrder
                        {
                            OrderId = order.OrderId,
                            OrderStatus = "Update"
                        }).ToArray()
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Err Update processing chunk: " + ex.ToString());
                throw;
            }
            
        }
    }
}
