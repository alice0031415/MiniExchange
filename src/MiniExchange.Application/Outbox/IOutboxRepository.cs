namespace MiniExchange.Application.Outbox;

public interface IOutboxRepository
{
    Task AddAsync(
        Guid id,
        string type,
        string key,
        string payload,
        DateTime createdAt,
        string? traceParent,
        string? traceState,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(
        int batchSize,
        CancellationToken cancellationToken);

    Task MarkPublishedAsync(
        Guid id,
        DateTime publishedAt,
        CancellationToken cancellationToken);
}