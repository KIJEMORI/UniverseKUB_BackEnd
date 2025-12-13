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
        protected override async Task ProcessMessages(OrderUnit[] messages)
        {
            
            using var scope = serviceProvider.CreateScope();
            
            var client = scope.ServiceProvider.GetRequiredService<OmsClient>();
            try
            {


                await client.LogOrder
                    (new V1AuditLogOrderRequest
                    {
                        Orders = messages.SelectMany(
                            order => (order.OrderItems as IEnumerable<OrderItemUnit> ?? Enumerable.Empty<OrderItemUnit>()).Select(
                                ol =>
                                {
                                    var res = new V1AuditLogOrderRequest.LogOrder
                                    {
                                        OrderId = order.Id,
                                        OrderItemId = ol.Id,
                                        CustomerId = order.CustomerId,
                                        OrderStatus = "Created"
                                    };
                                    return res;
                                }
                            )
                        )
                        .ToArray()
                    },
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Err Created "+ex.ToString());
                throw;
            }
        }
    }
}
