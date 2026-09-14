using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MiniExchange.KafkaConsumer.Data;

namespace MiniExchange.KafkaConsumer.Health;

public sealed class PostgresHealthCheck
    : IHealthCheck
{
    private readonly ConsumerDbContext _db;

    public PostgresHealthCheck(
        ConsumerDbContext db)
    {
        _db = db;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect =
                await _db.Database.CanConnectAsync(
                    cancellationToken);

            return canConnect
                ? HealthCheckResult.Healthy("PostgreSQL is available.")
                : HealthCheckResult.Unhealthy(
                    "PostgreSQL is unavailable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "PostgreSQL health check failed.",
                ex);
        }
    }
}