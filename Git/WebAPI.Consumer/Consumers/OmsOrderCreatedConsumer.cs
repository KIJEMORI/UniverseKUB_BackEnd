using Microsoft.Extensions.Options;
using RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using WebAPI.Config;
using Models;
using Models.Dto.V1.Requests;
using static Common.JsonSerializeExtensions;
using WebAPI.Clients;
using Models.Dto.V1.Responses;
using System.Diagnostics;
using Models.Dto.Common;

namespace WebAPI.Consumer.Consumers
{
    public class OmsOrderCreatedConsumer : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<RabbitMqSettings> _rabbitMqSettings;
        private readonly ConnectionFactory _factory;
        private IConnection _connection;
        private IChannel _channel;
        private AsyncEventingBasicConsumer _consumer;

        public OmsOrderCreatedConsumer(IOptions<RabbitMqSettings> rabbitMqSettings, IServiceProvider serviceProvider)
        {
            _rabbitMqSettings = rabbitMqSettings;
            _serviceProvider = serviceProvider;
            _factory = new ConnectionFactory { HostName = rabbitMqSettings.Value.HostName, Port = rabbitMqSettings.Value.Port };
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _connection = await _factory.CreateConnectionAsync(cancellationToken);
            _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await _channel.QueueDeclareAsync(
                queue: _rabbitMqSettings.Value.OrderCreatedQueue,
                durable: false,
                exclusive: false,
                autoDelete: false,
                arguments: null,
                cancellationToken: cancellationToken);

            var sw = new Stopwatch();

            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: cancellationToken);

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += async (sender, args) =>
            {

                sw.Restart();

                try
                {
                    var body = args.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var order = message.FromJson<OrderUnit>();

                    Console.WriteLine("Received: " + message);

                    using var scope = _serviceProvider.CreateScope();
                    var client = scope.ServiceProvider.GetRequiredService<OmsClient>();

                    /*List<V1AuditLogOrderRequest.LogOrder> request = new List<V1AuditLogOrderRequest.LogOrder>();
                    *//*foreach (var _order in order.Orders)
                    {*//*
                        foreach (var _itemorder in order.OrderItems)
                        {
                            request.Add(
                                new V1AuditLogOrderRequest.LogOrder
                                {
                                    OrderId = order.Id,
                                    OrderItemId = _itemorder.Id,
                                    CustomerId = order.CustomerId,
                                    OrderStatus = "Created"
                                }
                            );
                        }
                    //}*/

                   /* await client.LogOrder(new V1AuditLogOrderRequest
                    {
                        Orders = request.ToArray()
                    }, CancellationToken.None);*/

                    await client.LogOrder(new V1AuditLogOrderRequest
                    {
                        Orders = order.OrderItems.Select(x =>
                            new V1AuditLogOrderRequest.LogOrder
                            {
                                OrderId = order.Id,
                                OrderItemId = x.Id,
                                CustomerId = order.CustomerId,
                                OrderStatus = "Created"
                            }).ToArray()
                    }, CancellationToken.None);

                    await _channel.BasicAckAsync(args.DeliveryTag, false, cancellationToken);
                    sw.Stop();
                    Console.WriteLine($"Order created consumed in {sw.ElapsedMilliseconds} ms");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    await _channel.BasicNackAsync(args.DeliveryTag, false, true, cancellationToken);
                }

            };

            await _channel.BasicConsumeAsync(
                queue: _rabbitMqSettings.Value.OrderCreatedQueue,
                autoAck: false,
                consumer: _consumer,
                cancellationToken: cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
            _connection?.Dispose();
            _channel?.Dispose();
        }
    }
}
