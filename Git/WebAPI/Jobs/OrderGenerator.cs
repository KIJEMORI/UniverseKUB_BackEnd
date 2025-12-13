using AutoFixture;
using WebAPI.BLL.Models;
using WebAPI.BLL.Services;
using WebAPI.DAL.Models;

namespace WebAPI.Jobs
{
    public class OrderGenerator(IServiceProvider serviceProvider) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var fixture = new Fixture();
            using var scope = serviceProvider.CreateScope();
            var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

            while (!stoppingToken.IsCancellationRequested)
            {
                var orders = Enumerable.Range(1, 10)
                    .Select(_ =>
                    {
                        var orderItem = fixture.Build<OrderItemUnit>()
                            .With(x => x.PriceCurrency, "RUB")
                            .With(x => x.PriceCents, 1000)
                            .Create();

                        var order = fixture.Build<OrderUnit>()
                            .With(x => x.TotalPriceCurrency, "RUB")
                            .With(x => x.TotalPriceCents, 1000)
                            .With(x => x.OrderItems, [orderItem])
                            .Create();

                        return order;
                    })
                    .ToArray();

                await orderService.BatchInsert(orders, stoppingToken);

                var updates = Enumerable.Range(1, 10)
                    .Select(_ =>
                    {
                        Random rand = new Random();

                        var send = fixture.Build<V1UpdateOrderStatus>()
                            .With(x => x.Status, "Update")
                            .With(x => x.OrderIds, [rand.Next(5421, 9000)])
                            .Create();

                        return send;

                    })
                    .ToArray();

                await orderService.UpdateOrderStatus(updates, stoppingToken);

                await Task.Delay(250, stoppingToken);
            }
        }
    }
}
