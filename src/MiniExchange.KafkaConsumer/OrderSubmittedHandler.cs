using Microsoft.EntityFrameworkCore;
using MiniExchange.Contracts.Events;
using MiniExchange.KafkaConsumer.Data;

namespace MiniExchange.KafkaConsumer;

public sealed class OrderSubmittedHandler
    : IOrderSubmittedHandler
{
    private readonly ConsumerDbContext _db;
    private readonly InboxStore _inbox;

    public OrderSubmittedHandler(
        ConsumerDbContext db,
        InboxStore inbox)
    {
        _db = db;
        _inbox = inbox;
    }

    public async Task HandleAsync(
        EventEnvelope<OrderSubmitted> envelope,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        var inserted =
            await _inbox.TryAddAsync(
                envelope.EventId,
                envelope.EventType,
                envelope.OccurredAt,
                cancellationToken);

        if (inserted == 0)
        {
            await transaction.RollbackAsync(
                cancellationToken);

            return;
        }

        var order = envelope.Payload;

        var existing =
            await _db.OrderViews
                .SingleOrDefaultAsync(
                    x => x.OrderId == order.OrderId,
                    cancellationToken);

        if (existing is null)
        {
            _db.OrderViews.Add(
                new OrderViewEntity
                {
                    OrderId = order.OrderId,
                    InstrumentId = order.InstrumentId,
                    Side = order.Side,
                    Price = order.Price,
                    Quantity = order.Quantity,
                    RemainingQuantity = order.Quantity,
                    Status = "Active",
                    CreatedAt = order.CreatedAt
                });
        }

        await _db.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }
}