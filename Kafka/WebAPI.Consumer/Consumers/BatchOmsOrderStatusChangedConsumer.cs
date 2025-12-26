using Microsoft.Extensions.Options;
using WebAPI.Consumer.Base;
using WebAPI.Clients;
using Models.Dto.V1.Requests;
using WebAPI.Consumer.Config;
using WebAPI.DAL.Models;

namespace WebAPI.Consumer.Consumers;

public class BatchOmsOrderStatusChangedConsumer : BaseKafkaBatchConsumer<UpdateOrderStatus>
{
    private readonly IServiceProvider _serviceProvider;

    public BatchOmsOrderStatusChangedConsumer(
        IOptions<KafkaSettings> kafkaSettings,
        ILogger<BatchOmsOrderStatusChangedConsumer> logger,
        IServiceProvider serviceProvider)
        : base(kafkaSettings, kafkaSettings.Value.OmsOrderStatusChangedTopic, logger)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ProcessBatch(IEnumerable<Message<UpdateOrderStatus>> messages, CancellationToken token)
    {
        using var scope = _serviceProvider.CreateScope();
        var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

        var allLogs = messages
            .Where(m => m.Body?.OrderIds != null)
            .SelectMany(m => m.Body.OrderIds.Select(id => new V1AuditLogOrderRequest.LogOrder
            {
                OrderId = id,
                OrderItemId = 0,
                CustomerId = 0,
                OrderStatus = m.Body.Status
            }))
            .ToArray();

        if (allLogs.Any())
        {
            _logger.LogInformation("Processing Batch OmsOrderStatusChanged: Sending {Count} status updates to OmsClient", allLogs.Length);

            await client.LogOrder(new V1AuditLogOrderRequest
            {
                Orders = allLogs
            }, token);
        }
    }
}
