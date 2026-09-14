using Microsoft.EntityFrameworkCore;
using MiniExchange.Application.Outbox;
using MiniExchange.Infrastructure.Entities;

namespace MiniExchange.Infrastructure.Repositories;

public sealed class OutboxRepository : IOutboxRepository
{
    private readonly MiniExchangeDbContext _db;

    public OutboxRepository(MiniExchangeDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(
        Guid id,
        string type,
        string key,
        string payload,
        DateTime createdAt,
        string? traceParent,
        string? traceState,
        CancellationToken cancellationToken)
    {
        await _db.OutboxMessages.AddAsync(
            new OutboxMessageEntity
            {
                Id = id,
                Type = type,
                Key = key,
                Payload = payload,
                CreatedAt = createdAt,
                TraceParent = traceParent,
                TraceState = traceState
            },
            cancellationToken);
    }

    public async Task<IReadOnlyList<OutboxMessage>> GetUnpublishedAsync(
        int batchSize,
        CancellationToken cancellationToken)
    {
        return await _db.OutboxMessages
            .AsNoTracking()
            .Where(x => x.PublishedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(batchSize)
            .Select(x => new OutboxMessage(
                x.Id,
                x.Type,
                x.Key,
                x.Payload,
                x.CreatedAt,
                x.TraceParent,
                x.TraceState))
            .ToListAsync(cancellationToken);
    }

    public async Task MarkPublishedAsync(
        Guid id,
        DateTime publishedAt,
        CancellationToken cancellationToken)
    {
        await _db.OutboxMessages
            .Where(x => x.Id == id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(
                        x => x.PublishedAt,
                        publishedAt),
                cancellationToken);
    }
}