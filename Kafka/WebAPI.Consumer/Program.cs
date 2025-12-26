/*var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
*/

using WebAPI.Clients;
using WebAPI.Consumer.Config;
using WebAPI.Consumer.Consumers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KafkaSettings>(builder.Configuration.GetSection(nameof(KafkaSettings)));
builder.Services.Configure<HostOptions>(options =>
{
    options.ServicesStartConcurrently = true;
    options.ServicesStopConcurrently = true;
});

//builder.Services.AddHostedService<OmsOrderCreatedConsumer>();
//builder.Services.AddHostedService<OmsOrderStatusChangedConsumer>();

builder.Services.AddHostedService<BatchOmsOrderCreatedConsumer>();
builder.Services.AddHostedService<BatchOmsOrderStatusChangedConsumer>();
builder.Services.AddHttpClient<OmsClient>(
    c => {
        c.BaseAddress = new Uri(builder.Configuration["HttpClient:Oms:BaseAddress"]);
        c.Timeout = TimeSpan.FromMinutes(3);
    }
);

var app = builder.Build();
await app.RunAsync();