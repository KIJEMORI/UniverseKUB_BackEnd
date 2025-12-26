using Microsoft.Extensions.Options;
using WebAPI.Consumer.Base;
using WebAPI.Clients;
using Models.Dto.V1.Requests;
using WebAPI.Consumer.Config;
using WebAPI.DAL.Models;

namespace WebAPI.Consumer.Consumers
{

    public class OmsOrderStatusChangedConsumer : BaseKafkaConsumer<UpdateOrderStatus>
    {
        private readonly IServiceProvider _serviceProvider;

        public OmsOrderStatusChangedConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            ILogger<OmsOrderStatusChangedConsumer> logger,
            IServiceProvider serviceProvider)
            : base(kafkaSettings, kafkaSettings.Value.OmsOrderStatusChangedTopic, logger)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ProcessMessage(Message<UpdateOrderStatus> message, CancellationToken token)
        {
            var body = message.Body;

            if (body == null || body.OrderIds == null || body.OrderIds.Length == 0)
            {
                
                return;
            }

            using var scope = _serviceProvider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

            
            var request = new V1AuditLogOrderRequest
            {
                Orders = body.OrderIds.Select(orderId => new V1AuditLogOrderRequest.LogOrder
                {
                    OrderId = orderId,
                    OrderItemId = 0,
                    CustomerId = 0,
                    OrderStatus = body.Status
                }).ToArray()
            };

            await client.LogOrder(request, token);
        }
    }
}