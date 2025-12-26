using Microsoft.Extensions.Options;
using WebAPI.Consumer.Base;
using WebAPI.Clients;
using Models.Dto.V1.Requests;
using Models.Dto.Common;
using WebAPI.Consumer.Config;

namespace WebAPI.Consumer.Consumers
{

    public class BatchOmsOrderCreatedConsumer : BaseKafkaBatchConsumer<OrderUnit>
    {
        private readonly IServiceProvider _serviceProvider;

        public BatchOmsOrderCreatedConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            ILogger<BatchOmsOrderCreatedConsumer> logger,
            IServiceProvider serviceProvider)
            : base(kafkaSettings, kafkaSettings.Value.OmsOrderCreatedTopic, logger)
        {
            _serviceProvider = serviceProvider;
        }

        protected override async Task ProcessBatch(IEnumerable<Message<OrderUnit>> messages, CancellationToken token)
        {
            using var scope = _serviceProvider.CreateScope();
            var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

            var allLogs = messages
                .Where(m => m.Body != null)
                .SelectMany(orderMsg => orderMsg.Body.OrderItems.Select(item => new V1AuditLogOrderRequest.LogOrder
                {
                    OrderId = orderMsg.Body.Id,
                    OrderItemId = item.Id,
                    CustomerId = orderMsg.Body.CustomerId,
                    OrderStatus = "Created"
                }))
                .ToArray();

            if (allLogs.Any())
            {
                _logger.LogInformation("Processing Batch OmsOrderCreated: Sending {Count} items to OmsClient", allLogs.Length);

                await client.LogOrder(new V1AuditLogOrderRequest
                {
                    Orders = allLogs
                }, token);
            }
        }
    }
}