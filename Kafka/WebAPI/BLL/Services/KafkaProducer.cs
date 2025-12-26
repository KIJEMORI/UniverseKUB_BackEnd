using Common;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using WebAPI.Config;

namespace WebAPI.BLL.Services
{
    public class KafkaProducer
    {
        private readonly IProducer<string, string> _producer;

        public KafkaProducer(IOptions<KafkaSettings> kafkaSettings)
        {
            var config = new ProducerConfig
            {
                BootstrapServers = kafkaSettings.Value.BootstrapServers,
                ClientId = kafkaSettings.Value.ClientId,
                LingerMs = 100,
                CompressionType = CompressionType.Snappy,
                Partitioner = Confluent.Kafka.Partitioner.ConsistentRandom
            };

            _producer = new ProducerBuilder<string, string>(config).Build();
        }

        public async Task Produce<T>(string topic, (string key, T message)[] messages, CancellationToken token)
        {
            var tasks = messages.Select(async message =>
            {
                try
                {
                    return await _producer.ProduceAsync(topic,
                        new Message<string, string>
                        {
                            Key = message.key,
                            Value = message.message.ToJson()
                        }, token);
                }
                catch (ProduceException<string, string> ex)
                {
                    Console.WriteLine($"Failed to send message: {ex.Error.Reason}");
                    return null;
                }
            });

            var results = await Task.WhenAll(tasks);

            if (results.Any(x => x is null))
            {
                throw new Exception("Failed to produce messages");
            }
        }
    }
}
