using Microsoft.EntityFrameworkCore;
using MiniExchange.KafkaConsumer.Data;

namespace MiniExchange.KafkaConsumer;

internal static class InboxMessageExtensions
{
    public static void AddInboxMessage(
        this ConsumerDbContext db,
        Guid eventId,
        string eventType,
        DateTime receivedAt)
    {
        db.InboxMessages.Add(
            new InboxMessageEntity
            {
                EventId = eventId,
                EventType = eventType,
                ReceivedAt = receivedAt,
                ProcessedAt = DateTime.UtcNow
            });
    }
}