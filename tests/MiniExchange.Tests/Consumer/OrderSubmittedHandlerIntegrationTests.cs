using Microsoft.EntityFrameworkCore;
using MiniExchange.Contracts.Events;
using MiniExchange.KafkaConsumer;
using MiniExchange.KafkaConsumer.Data;
using Testcontainers.PostgreSql;

namespace MiniExchange.Tests.Consumer;

public sealed class OrderSubmittedHandlerIntegrationTests
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
    public async Task HandleAsync_CreatesOrderView()
    {
        var inbox = new InboxStore(_db);

        var handler =
            new OrderSubmittedHandler(
                _db,
                inbox);

        var orderId = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();

        var payload = new OrderSubmitted(
            orderId,
            instrumentId,
            "Buy",
            250m,
            100m,
            DateTime.UtcNow);

        var envelope =
            new EventEnvelope<OrderSubmitted>(
                Guid.NewGuid(),
                nameof(OrderSubmitted),
                1,
                DateTime.UtcNow,
                payload);

        await handler.HandleAsync(
            envelope,
            CancellationToken.None);

        var order =
            await _db.OrderViews
                .SingleAsync();

        Assert.Equal(
            orderId,
            order.OrderId);

        Assert.Equal(
            instrumentId,
            order.InstrumentId);

        Assert.Equal(
            100m,
            order.RemainingQuantity);

        Assert.Equal(
            "Active",
            order.Status);

        Assert.True(
            await _db.InboxMessages.AnyAsync(
                x => x.EventId == envelope.EventId));
    }

    [Fact]
    public async Task HandleAsync_DuplicateEvent_DoesNotCreateSecondOrderView()
    {
        var inbox = new InboxStore(_db);

        var handler =
            new OrderSubmittedHandler(
                _db,
                inbox);

        var orderId = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();

        var payload = new OrderSubmitted(
            orderId,
            instrumentId,
            "Buy",
            250m,
            100m,
            DateTime.UtcNow);

        var envelope =
            new EventEnvelope<OrderSubmitted>(
                Guid.NewGuid(),
                nameof(OrderSubmitted),
                1,
                DateTime.UtcNow,
                payload);

        await handler.HandleAsync(
            envelope,
            CancellationToken.None);

        await handler.HandleAsync(
            envelope,
            CancellationToken.None);

        var orders =
            await _db.OrderViews
                .Where(x => x.OrderId == orderId)
                .ToListAsync();

        var inboxMessages =
            await _db.InboxMessages
                .Where(x => x.EventId == envelope.EventId)
                .ToListAsync();

        Assert.Single(orders);
        Assert.Single(inboxMessages);
    }

    [Fact]
    public async Task HandleAsync_WhenOrderViewInsertFails_RollsBackInbox()
    {
        var options =
            new DbContextOptionsBuilder<ConsumerDbContext>()
                .UseNpgsql(_postgres.GetConnectionString())
                .Options;

        await using var db =
            new ConsumerDbContext(options);

        var inbox = new InboxStore(db);

        var handler =
            new OrderSubmittedHandler(
                db,
                inbox);

        var eventId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var instrumentId = Guid.NewGuid();

        var envelope =
            new EventEnvelope<OrderSubmitted>(
                eventId,
                nameof(OrderSubmitted),
                1,
                DateTime.UtcNow,
                new OrderSubmitted(
                    orderId,
                    instrumentId,
                    null!,
                    250m,
                    100m,
                    DateTime.UtcNow));

        await Assert.ThrowsAsync<DbUpdateException>(
            () => handler.HandleAsync(
                envelope,
                CancellationToken.None));

        var inboxExists =
            await db.InboxMessages.AnyAsync(
                x => x.EventId == eventId);

        var orderExists =
            await db.OrderViews.AnyAsync(
                x => x.OrderId == orderId);

        Assert.False(inboxExists);
        Assert.False(orderExists);
    }
}