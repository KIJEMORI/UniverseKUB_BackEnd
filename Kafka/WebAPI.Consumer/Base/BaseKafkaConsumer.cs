using Confluent.Kafka;
using Microsoft.Extensions.Options;
using WebAPI.Consumer.Base;
using WebAPI.Consumer.Config;
using Common;

namespace WebAPI.Consumer.Base;

// 1. Наследуемся от BackgroundService
public abstract class BaseKafkaConsumer<T> : BackgroundService where T : class
{
    private readonly IConsumer<string, string> _consumer;
    private readonly string _topic;
    protected readonly ILogger<BaseKafkaConsumer<T>> _logger;

    protected BaseKafkaConsumer(
        IOptions<KafkaSettings> kafkaSettings,
        string topic,
        ILogger<BaseKafkaConsumer<T>> logger)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = kafkaSettings.Value.BootstrapServers,
            GroupId = kafkaSettings.Value.GroupId,
            AutoOffsetReset = AutoOffsetReset.Latest,
            EnableAutoCommit = true,
            AutoCommitIntervalMs = 5_000,
            SessionTimeoutMs = 60_000,
            HeartbeatIntervalMs = 3_000,
            MaxPollIntervalMs = 300_000
        };

        _logger = logger;
        _topic = topic;
        _consumer = new ConsumerBuilder<string, string>(config).Build();
    }

 
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _consumer.Subscribe(_topic);
        _logger.LogInformation("Started consuming from topic: {Topic}", _topic);

     
        await Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                   
                    var consumeResult = _consumer.Consume(stoppingToken);
                    if (consumeResult?.Message == null) continue;

                    var msg = new Message<T>
                    {
                        Key = consumeResult.Message.Key,
                        Body = consumeResult.Message.Value.FromJson<T>()
                    };

                    await ProcessMessage(msg, stoppingToken);

                    
                    _consumer.Commit(consumeResult);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error processing message from {Topic}", _topic);
                }
            }
        }, stoppingToken);
    }

    protected abstract Task ProcessMessage(Message<T> message, CancellationToken token);

    public override void Dispose()
    {
        _consumer.Close();
        _consumer.Dispose();
        base.Dispose();
    }
}
