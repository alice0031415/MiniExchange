using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MiniExchange.KafkaConsumer.Health;

public sealed class KafkaHealthCheck
    : IHealthCheck
{
    private readonly IAdminClient _adminClient;
    private readonly string _topic;

    public KafkaHealthCheck(
        IAdminClient adminClient,
        IConfiguration configuration)
    {
        _adminClient = adminClient;

        _topic =
            configuration["Kafka:Topic"]
            ?? "orders";
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata =
                _adminClient.GetMetadata(
                    _topic,
                    TimeSpan.FromSeconds(3));

            var topic =
                metadata.Topics
                    .FirstOrDefault(x => x.Topic == _topic);

            if (topic is null ||
                topic.Error.IsError)
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy(
                        $"Kafka topic '{_topic}' is unavailable."));
            }

            return Task.FromResult(
                HealthCheckResult.Healthy(
                    "Kafka is available."));
        }
        catch (Exception ex)
        {
            return Task.FromResult(
                HealthCheckResult.Unhealthy(
                    "Kafka health check failed.",
                    ex));
        }
    }
}