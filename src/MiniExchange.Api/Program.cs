using Confluent.Kafka;
using Microsoft.EntityFrameworkCore;
using MiniExchange.Application;
using MiniExchange.Application.Instruments;
using MiniExchange.Application.Orders;
using MiniExchange.Application.Outbox;
using MiniExchange.Application.Trades;
using MiniExchange.Domain;
using MiniExchange.Infrastructure;
using MiniExchange.Infrastructure.Messaging;
using MiniExchange.Infrastructure.Repositories;
using System.Text.Json.Serialization;
using StackExchange.Redis;
using MiniExchange.Application.OrderBooks;
using MiniExchange.Infrastructure.Redis;
using MiniExchange.Application.Concurrency;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;
using Prometheus;
using MiniExchange.Application.Metrics;
using MiniExchange.Infrastructure.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService("MiniExchange.Api"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("MiniExchange.Trading")
            .AddSource("MiniExchange.Infrastructure")
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddOtlpExporter(options =>
            {
                options.Endpoint =
                    new Uri(
                        builder.Configuration[
                            "OpenTelemetry:OtlpEndpoint"]!);

                options.Protocol =
                    OtlpExportProtocol.HttpProtobuf;
            });
    });

builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddHealthChecks();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MiniExchangeDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString(
            "DefaultConnection"));
});

builder.Services.AddSingleton<MatchingEngine>();

builder.Services
    .AddOptions<KafkaOptions>()
    .BindConfiguration("Kafka");

builder.Services.AddSingleton<IProducer<string, string>>(sp =>
{
    var options = sp
        .GetRequiredService<
            Microsoft.Extensions.Options.IOptions<KafkaOptions>>()
        .Value;

    var config = new ProducerConfig
    {
        BootstrapServers = options.BootstrapServers
    };

    return new ProducerBuilder<string, string>(config)
        .Build();
});

builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")!));

builder.Services.AddSingleton<
    IOrderBookCache,
    RedisOrderBookCache>();

builder.Services.AddSingleton<
    IDistributedLock,
    RedisDistributedLock>();

builder.Services.AddSingleton<
    ITradingMetrics,
    PrometheusTradingMetrics>();

builder.Services.AddScoped<
    IInstrumentRepository,
    InstrumentRepository>();

builder.Services.AddScoped<
    IOrderRepository,
    OrderRepository>();

builder.Services.AddScoped<
    ITradeRepository,
    TradeRepository>();

builder.Services.AddScoped<
    IOutboxRepository,
    OutboxRepository>();

builder.Services.AddScoped<
    IUnitOfWork,
    UnitOfWork>();

builder.Services.AddScoped<
    ITradingService,
    TradingService>();

builder.Services.AddScoped<
    IOrderBookService,
    OrderBookService>();

builder.Services.AddHostedService<OutboxPublisher>();

var app = builder.Build();

app.UseHttpMetrics();

await using (var scope =
    app.Services.CreateAsyncScope())
{
    var db =
        scope.ServiceProvider
            .GetRequiredService<MiniExchangeDbContext>();

    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapMetrics();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();