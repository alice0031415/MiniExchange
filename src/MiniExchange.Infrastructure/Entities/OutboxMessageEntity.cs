namespace MiniExchange.Infrastructure.Entities;

public class OutboxMessageEntity
{
    public Guid Id { get; set; }

    public string Type { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? PublishedAt { get; set; }

    public string? TraceParent { get; set; }

    public string? TraceState { get; set; }
}