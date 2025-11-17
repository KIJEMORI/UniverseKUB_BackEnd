using Microsoft.Extensions.Options;
using Models.Dto.V1.Requests;
using WebAPI.BLL.Models;
using WebAPI.Clients;
using WebAPI.Config;
using WebAPI.Consumer.Base;

namespace WebAPI.Consumer.Consumers
{
    public class BatchOmsOrderCreatedConsumer(
    IOptions<RabbitMqSettings> rabbitMqSettings,
    IServiceProvider serviceProvider)
    : BaseBatchMessageConsumer<OrderUnit>(rabbitMqSettings.Value)
    {
        protected override async Task ProcessMessages(OrderUnit[] messages)
        {
            using var scope = serviceProvider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

            await client.LogOrder(new V1AuditLogOrderRequest
            {
                Orders = messages.SelectMany(order => order.OrderItems.Select(ol =>
                    new V1AuditLogOrderRequest.LogOrder
                    {
                        OrderId = order.Id,
                        OrderItemId = ol.Id,
                        CustomerId = order.CustomerId,
                        OrderStatus = "Created"
                    })).ToArray()
            }, CancellationToken.None);
        }
    }
}
