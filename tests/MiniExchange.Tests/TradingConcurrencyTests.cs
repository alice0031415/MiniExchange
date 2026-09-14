using Microsoft.EntityFrameworkCore;
using MiniExchange.Application;
using MiniExchange.Domain;
using MiniExchange.Infrastructure;
using MiniExchange.Infrastructure.Repositories;
using MiniExchange.Infrastructure.Messaging;
using StackExchange.Redis;
using MiniExchange.Infrastructure.Redis;
using MiniExchange.Infrastructure.Metrics;

namespace MiniExchange.Tests;

public class TradingConcurrencyTests
{
    private const string ConnectionString =
        "Host=localhost;" +
        "Port=5433;" +
        "Database=miniexchange;" +
        "Username=miniexchange;" +
        "Password=miniexchange";

    [Fact]
    public async Task ConcurrentOrders_ShouldNotOversell()
    {
        var options =
            new DbContextOptionsBuilder<MiniExchangeDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;

        var instrumentId = Guid.NewGuid();
        var sellOrderId = Guid.NewGuid();

        await using (var db = new MiniExchangeDbContext(options))
        {
            db.Instruments.Add(new Infrastructure.Entities.InstrumentEntity
            {
                Id = instrumentId,
                Ticker = $"T{Guid.NewGuid():N}"[..10],
                Name = "Concurrency Test"
            });

            db.Orders.Add(new Infrastructure.Entities.OrderEntity
            {
                Id = sellOrderId,
                InstrumentId = instrumentId,
                Side = (int)OrderSide.Sell,
                Price = 250m,
                Quantity = 100m,
                RemainingQuantity = 100m,
                Status = (int)OrderStatus.Active,
                CreatedAt = DateTime.UtcNow,
                Version = 1
            });

            await db.SaveChangesAsync();
        }

        var task1 = PlaceOrder(
            options,
            instrumentId,
            OrderSide.Buy,
            60m);

        var task2 = PlaceOrder(
            options,
            instrumentId,
            OrderSide.Buy,
            60m);

        await Task.WhenAll(task1, task2);

        await using var verificationDb =
            new MiniExchangeDbContext(options);

        var trades = await verificationDb.Trades
            .Where(x => x.InstrumentId == instrumentId)
            .ToListAsync();

        var sellOrder = await verificationDb.Orders
            .SingleAsync(x => x.Id == sellOrderId);

        Assert.Equal(
            100m,
            trades.Sum(x => x.Quantity));

        Assert.Equal(
            OrderStatus.Filled,
            (OrderStatus)sellOrder.Status);

        Assert.Equal(
            0m,
            sellOrder.RemainingQuantity);
    }

    private static async Task<MiniExchange.Domain.Order> PlaceOrder(
        DbContextOptions<MiniExchangeDbContext> options,
        Guid instrumentId,
        OrderSide side,
        decimal quantity)
    {
        await using var db =
            new MiniExchangeDbContext(options);

        var service = new TradingService(
            new InstrumentRepository(db),
            new OrderRepository(db),
            new TradeRepository(db),
            new OutboxRepository(db),
            new UnitOfWork(db),
            new MatchingEngine(),
            new RedisOrderBookCache(ConnectionMultiplexer.Connect("localhost:6379")),
            new PrometheusTradingMetrics());

        return await service.PlaceOrder(
            instrumentId,
            side,
            250m,
            quantity,
            CancellationToken.None);
    }
}