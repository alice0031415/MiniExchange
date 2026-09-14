using Confluent.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MiniExchange.KafkaConsumer;
using MiniExchange.KafkaConsumer.Data;
using MiniExchange.KafkaConsumer.Health;
using OpenTelemetry.Exporter;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService("MiniExchange.KafkaConsumer"))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource("MiniExchange.KafkaConsumer")
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

builder.WebHost.ConfigureKestrel(options =>
{
    options.Listen(
        System.Net.IPAddress.Any,
        5000,
        listenOptions =>
        {
            listenOptions.Protocols =
                HttpProtocols.Http2;
        });

    options.Listen(
        System.Net.IPAddress.Any,
        5001,
        listenOptions =>
        {
            listenOptions.Protocols =
                HttpProtocols.Http1;
        });
});

builder.Services.AddGrpc();

builder.Services.AddGrpcHealthChecks();

builder.Services
    .AddHealthChecks()
    .AddCheck(
        "liveness",
        () => HealthCheckResult.Healthy(),
        tags: new[] { "live" })
    .AddCheck<PostgresHealthCheck>(
        "postgres",
        tags: new[] { "ready" })
    .AddCheck<KafkaHealthCheck>(
        "kafka",
        tags: new[] { "ready" });

builder.Services
    .AddOptions<KafkaOptions>()
    .BindConfiguration("Kafka");

builder.Services.AddDbContext<ConsumerDbContext>(
    options =>
    {
        options.UseNpgsql(
            builder.Configuration.GetConnectionString(
                "DefaultConnection"));
    });

builder.Services.AddSingleton<IAdminClient>(_ =>
{
    var config = new AdminClientConfig
    {
        BootstrapServers =
            builder.Configuration["Kafka:BootstrapServers"]
            ?? "localhost:9092"
    };

    return new AdminClientBuilder(config)
        .Build();
});

builder.Services.AddSingleton<
    IProducer<string, string>>(_ =>
    {
        var config = new ProducerConfig
        {
            BootstrapServers =
                builder.Configuration["Kafka:BootstrapServers"]
                ?? "localhost:9092"
        };

        return new ProducerBuilder<string, string>(
            config)
            .Build();
    });

builder.Services.AddScoped<InboxStore>();

builder.Services.AddScoped<
    IOrderSubmittedHandler,
    OrderSubmittedHandler>();

builder.Services.AddScoped<
    IOrderStateChangedHandler,
    OrderStateChangedHandler>();

builder.Services.AddScoped<
    ITradeExecutedHandler,
    TradeExecutedHandler>();

builder.Services.AddHostedService<
    KafkaConsumerWorker>();

var app = builder.Build();

app.UseHttpMetrics();

await using (var scope =
    app.Services.CreateAsyncScope())
{
    var db =
        scope.ServiceProvider
            .GetRequiredService<ConsumerDbContext>();

    await db.Database.MigrateAsync();
}

// gRPC API.
app.MapGrpcService<OrderQueryGrpcService>();

// gRPC health API.
app.MapGrpcHealthChecksService();

// Liveness: process is alive.
app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = check =>
            check.Tags.Contains("live")
    });

// Readiness: dependencies are available.
app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = check =>
            check.Tags.Contains("ready")
    });

app.MapGet(
    "/",
    () => "MiniExchange Kafka Consumer");

app.MapMetrics();

await app.RunAsync();