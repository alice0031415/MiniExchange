using Microsoft.EntityFrameworkCore;
using MiniExchange.Contracts.Events;
using MiniExchange.KafkaConsumer.Data;

namespace MiniExchange.KafkaConsumer;

public sealed class TradeExecutedHandler
    : ITradeExecutedHandler
{
    private readonly ConsumerDbContext _db;
    private readonly InboxStore _inbox;

    public TradeExecutedHandler(
        ConsumerDbContext db,
        InboxStore inbox)
    {
        _db = db;
        _inbox = inbox;
    }

    public async Task HandleAsync(
        EventEnvelope<TradeExecuted> envelope,
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

        var trade = envelope.Payload;

        var tradeView =
            await _db.TradeViews
                .SingleOrDefaultAsync(
                    x => x.TradeId == trade.TradeId,
                    cancellationToken);

        if (tradeView is null)
        {
            _db.TradeViews.Add(
                new TradeViewEntity
                {
                    TradeId = trade.TradeId,
                    InstrumentId = trade.InstrumentId,
                    BuyOrderId = trade.BuyOrderId,
                    SellOrderId = trade.SellOrderId,
                    Price = trade.Price,
                    Quantity = trade.Quantity,
                    ExecutedAt = trade.ExecutedAt
                });
        }

        await _db.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);
    }
}