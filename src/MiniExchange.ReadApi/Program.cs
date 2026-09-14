using Grpc.Net.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MiniExchange.Contracts.Grpc;
using MiniExchange.ReadApi.Services;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Exporter;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
        resource.AddService("MiniExchange.ReadApi"))
    .WithTracing(tracing =>
    {
        tracing
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

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var grpcAddress =
    builder.Configuration["Grpc:OrderQueryAddress"]
    ?? "http://localhost:5000";

var channel =
    GrpcChannel.ForAddress(grpcAddress);

builder.Services.AddSingleton(channel);

builder.Services.AddSingleton(
    new OrderQuery.OrderQueryClient(channel));

builder.Services.AddSingleton<OrderQueryClient>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.MapHealthChecks(
    "/health/live",
    new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });

app.MapHealthChecks(
    "/health/ready");

app.Run();