using MiniExchange.Application.Concurrency;
using StackExchange.Redis;

namespace MiniExchange.Infrastructure.Redis;

public sealed class RedisDistributedLock
    : IDistributedLock
{
    private readonly IConnectionMultiplexer _redis;

    public RedisDistributedLock(
        IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken)
    {
        var database = _redis.GetDatabase();

        var lockKey = $"lock:{key}";
        var lockValue = Guid.NewGuid().ToString();

        var acquired = await database.StringSetAsync(
            lockKey,
            lockValue,
            expiration,
            When.NotExists);

        if (!acquired)
            return null;

        return new RedisLockHandle(
            database,
            lockKey,
            lockValue);
    }

    private sealed class RedisLockHandle
        : IAsyncDisposable
    {
        private readonly IDatabase _database;
        private readonly string _key;
        private readonly string _value;

        public RedisLockHandle(
            IDatabase database,
            string key,
            string value)
        {
            _database = database;
            _key = key;
            _value = value;
        }

        public async ValueTask DisposeAsync()
        {
            const string script = """
                if redis.call('GET', KEYS[1]) == ARGV[1] then
                    return redis.call('DEL', KEYS[1])
                end
                return 0
                """;

            await _database.ScriptEvaluateAsync(
                script,
                new RedisKey[] { _key },
                new RedisValue[] { _value });
        }
    }
}