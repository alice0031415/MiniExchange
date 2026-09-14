namespace MiniExchange.Application.Outbox;

public sealed record OutboxMessage(
    Guid Id,
    string Type,
    string Key,
    string Payload,
    DateTime CreatedAt,
    string? TraceParent,
    string? TraceState);