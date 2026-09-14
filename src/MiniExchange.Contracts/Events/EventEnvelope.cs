namespace MiniExchange.Contracts.Events;

public sealed record EventEnvelope<T>(
    Guid EventId,
    string EventType,
    int Version,
    DateTime OccurredAt,
    T Payload);