using Microsoft.Extensions.Options;
using WebAPI.Consumer.Base;
using WebAPI.Clients;
using Models.Dto.V1.Requests;
using Models.Dto.Common;
using WebAPI.Consumer.Config; // Предполагаю, что OrderUnit здесь

namespace WebAPI.Consumer.Consumers
{

    public class OmsOrderCreatedConsumer : BaseKafkaConsumer<OrderUnit>
    {
        private readonly IServiceProvider _serviceProvider;

        public OmsOrderCreatedConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            ILogger<OmsOrderCreatedConsumer> logger,
            IServiceProvider serviceProvider)
            : base(kafkaSettings, kafkaSettings.Value.OmsOrderCreatedTopic, logger)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ProcessMessage(Message<OrderUnit> message, CancellationToken token)
        {
            var order = message.Body;

            if (order == null)
            {

                return;
            }


            using var scope = _serviceProvider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

            var request = new V1AuditLogOrderRequest
            {
                Orders = order.OrderItems.Select(x => new V1AuditLogOrderRequest.LogOrder
                {
                    OrderId = order.Id,
                    OrderItemId = x.Id,
                    CustomerId = order.CustomerId,
                    OrderStatus = "Created"
                }).ToArray()
            };

            await client.LogOrder(request, token);
        }
    }
}
