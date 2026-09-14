using MiniExchange.Application.OrderBooks;
using StackExchange.Redis;
using System.Text.Json;

namespace MiniExchange.Infrastructure.Redis;

public sealed class RedisOrderBookCache
    : IOrderBookCache
{
    private readonly IDatabase _db;

    public RedisOrderBookCache(
        IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task SetAsync(
        Guid instrumentId,
        OrderBookSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        var key = GetKey(instrumentId);

        var json = JsonSerializer.Serialize(snapshot);

        await _db.StringSetAsync(
            key,
            json);
    }

    public async Task<OrderBookSnapshot?> GetAsync(
        Guid instrumentId,
        CancellationToken cancellationToken)
    {
        var value = await _db.StringGetAsync(
            GetKey(instrumentId));

        if (value.IsNullOrEmpty)
            return null;

        return JsonSerializer.Deserialize<OrderBookSnapshot>(
            value.ToString());
    }

    public Task RemoveAsync(
        Guid instrumentId,
        CancellationToken cancellationToken)
    {
        return _db.KeyDeleteAsync(
            GetKey(instrumentId));
    }

    private static string GetKey(
        Guid instrumentId)
    {
        return $"orderbook:{instrumentId}";
    }
}