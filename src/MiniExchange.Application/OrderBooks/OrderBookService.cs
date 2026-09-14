using MiniExchange.Application.Orders;
using MiniExchange.Domain;

namespace MiniExchange.Application.OrderBooks;

public sealed class OrderBookService : IOrderBookService
{
    private readonly IOrderRepository _orders;
    private readonly IOrderBookCache _cache;

    public OrderBookService(
        IOrderRepository orders,
        IOrderBookCache cache)
    {
        _orders = orders;
        _cache = cache;
    }

    public async Task<OrderBookSnapshot> GetAsync(
        Guid instrumentId,
        CancellationToken cancellationToken)
    {
        var cached = await _cache.GetAsync(
            instrumentId,
            cancellationToken);

        if (cached is not null)
            return cached;

        var orders = await _orders.GetActiveOrdersAsync(
            instrumentId,
            cancellationToken);

        var bids = orders
            .Where(x => x.Side == OrderSide.Buy)
            .GroupBy(x => x.Price)
            .OrderByDescending(x => x.Key)
            .Select(x => new OrderBookLevel(
                x.Key,
                x.Sum(o => o.RemainingQuantity)))
            .ToList();

        var asks = orders
            .Where(x => x.Side == OrderSide.Sell)
            .GroupBy(x => x.Price)
            .OrderBy(x => x.Key)
            .Select(x => new OrderBookLevel(
                x.Key,
                x.Sum(o => o.RemainingQuantity)))
            .ToList();

        var snapshot = new OrderBookSnapshot(
            bids,
            asks);

        await _cache.SetAsync(
            instrumentId,
            snapshot,
            cancellationToken);

        return snapshot;
    }
}