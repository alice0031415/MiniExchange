using Microsoft.EntityFrameworkCore;
using MiniExchange.Contracts.Events;
using MiniExchange.KafkaConsumer;
using MiniExchange.KafkaConsumer.Data;
using Testcontainers.PostgreSql;

namespace MiniExchange.Tests.Consumer;

public sealed class TradeExecutedHandlerIntegrationTests
    : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder()
            .WithDatabase("consumer_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();

    private ConsumerDbContext _db = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var options =
            new DbContextOptionsBuilder<ConsumerDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

        _db = new ConsumerDbContext(options);

        await _db.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _db.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task HandleAsync_CreatesTradeViewAndInbox()
    {
        var inbox = new InboxStore(_db);

        var handler =
            new TradeExecutedHandler(
                _db,
                inbox);

        var tradeId = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();
        var buyOrderId = Guid.NewGuid();
        var sellOrderId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var envelope =
            new EventEnvelope<TradeExecuted>(
                Guid.NewGuid(),
                nameof(TradeExecuted),
                1,
                occurredAt,
                new TradeExecuted(
                    tradeId,
                    instrumentId,
                    buyOrderId,
                    sellOrderId,
                    250m,
                    60m,
                    occurredAt));

        await handler.HandleAsync(
            envelope,
            CancellationToken.None);

        var trade =
            await _db.TradeViews
                .SingleAsync(
                    x => x.TradeId == tradeId);

        Assert.Equal(
            instrumentId,
            trade.InstrumentId);

        Assert.Equal(
            buyOrderId,
            trade.BuyOrderId);

        Assert.Equal(
            sellOrderId,
            trade.SellOrderId);

        Assert.Equal(
            250m,
            trade.Price);

        Assert.Equal(
            60m,
            trade.Quantity);

        Assert.True(
            await _db.InboxMessages.AnyAsync(
                x => x.EventId == envelope.EventId));
    }

    [Fact]
    public async Task HandleAsync_DuplicateEvent_DoesNotCreateSecondTrade()
    {
        var inbox = new InboxStore(_db);

        var handler =
            new TradeExecutedHandler(
                _db,
                inbox);

        var tradeId = Guid.NewGuid();
        var occurredAt = DateTime.UtcNow;

        var envelope =
            new EventEnvelope<TradeExecuted>(
                Guid.NewGuid(),
                nameof(TradeExecuted),
                1,
                occurredAt,
                new TradeExecuted(
                    tradeId,
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    250m,
                    60m,
                    occurredAt));

        await handler.HandleAsync(
            envelope,
            CancellationToken.None);

        await handler.HandleAsync(
            envelope,
            CancellationToken.None);

        var trades =
            await _db.TradeViews
                .Where(x => x.TradeId == tradeId)
                .ToListAsync();

        var inboxCount =
            await _db.InboxMessages.CountAsync(
                x => x.EventId == envelope.EventId);

        Assert.Single(trades);
        Assert.Equal(1, inboxCount);
    }
}