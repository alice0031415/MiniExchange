namespace MiniExchange.KafkaConsumer.Data;

public sealed class InboxMessageEntity
{
    public Guid EventId { get; set; }

    public string EventType { get; set; } = null!;

    public DateTime ReceivedAt { get; set; }

    public DateTime ProcessedAt { get; set; }
}