using Confluent.Kafka;
using Microsoft.Extensions.Options;
using WebAPI.Consumer.Config;
using Common;

namespace WebAPI.Consumer.Base
{

    public abstract class BaseKafkaBatchConsumer<T> : BackgroundService where T : class
    {
        private readonly IConsumer<string, string> _consumer;
        private readonly string _topic;
        private readonly int _batchSize;
        private readonly int _timeoutMs;
        protected readonly ILogger<BaseKafkaBatchConsumer<T>> _logger;

        protected BaseKafkaBatchConsumer(
            IOptions<KafkaSettings> kafkaSettings,
            string topic,
            ILogger<BaseKafkaBatchConsumer<T>> logger)
        {
            _logger = logger;
            _topic = topic;
            // Берем значения напрямую из конфига
            _batchSize = kafkaSettings.Value.CollectBatchSize;
            _timeoutMs = kafkaSettings.Value.CollectTimeoutMs;

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

            _consumer = new ConsumerBuilder<string, string>(config).Build();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_topic);
            _logger.LogInformation("Started Batch Consumer for {Topic} (Batch: {BatchSize}, Timeout: {Timeout}ms)",
                _topic, _batchSize, _timeoutMs);

            var batch = new List<ConsumeResult<string, string>>();
            var lastFlush = DateTime.UtcNow;

            await Task.Run(async () =>
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        
                        var result = _consumer.Consume(TimeSpan.FromMilliseconds(100));
                        if (result != null) batch.Add(result);

                        var elapsed = (DateTime.UtcNow - lastFlush).TotalMilliseconds;

                        if (batch.Count > 0 && (batch.Count >= _batchSize || elapsed >= _timeoutMs || stoppingToken.IsCancellationRequested))
                        {
                            await ProcessBatchInternal(batch, stoppingToken);
                            batch.Clear();
                            lastFlush = DateTime.UtcNow;
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in Batch Consumer loop for {Topic}", _topic);
                        await Task.Delay(1000, stoppingToken);
                    }
                }
            }, stoppingToken);
        }

        private async Task ProcessBatchInternal(List<ConsumeResult<string, string>> rawMessages, CancellationToken token)
        {
            var messages = rawMessages.Select(m => new Message<T>
            {
                Key = m.Message.Key,
                Body = m.Message.Value.FromJson<T>()
            }).ToList();

            try
            {
                await ProcessBatch(messages, token);
                _consumer.Commit(rawMessages.Last());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process batch for {Topic}. Retrying in next cycle...", _topic);
                
            }
        }

        protected abstract Task ProcessBatch(IEnumerable<Message<T>> messages, CancellationToken token);

        public override void Dispose()
        {
            _consumer.Close();
            _consumer.Dispose();
            base.Dispose();
        }
    }
}
