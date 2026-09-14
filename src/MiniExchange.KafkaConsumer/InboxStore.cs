using Microsoft.EntityFrameworkCore;
using MiniExchange.Contracts.Events;
using MiniExchange.KafkaConsumer.Data;

namespace MiniExchange.KafkaConsumer;

public sealed class InboxStore
{
    private readonly ConsumerDbContext _db;

    public InboxStore(
        ConsumerDbContext db)
    {
        _db = db;
    }

    public Task<int> TryAddAsync(
        Guid eventId,
        string eventType,
        DateTime receivedAt,
        CancellationToken cancellationToken)
    {
        return _db.Database.ExecuteSqlInterpolatedAsync(
            $"""
            INSERT INTO "InboxMessages"
                (
                    "EventId",
                    "EventType",
                    "ReceivedAt",
                    "ProcessedAt"
                )
            VALUES
                (
                    {eventId},
                    {eventType},
                    {receivedAt},
                    {DateTime.UtcNow}
                )
            ON CONFLICT ("EventId") DO NOTHING
            """,
            cancellationToken);
    }
}