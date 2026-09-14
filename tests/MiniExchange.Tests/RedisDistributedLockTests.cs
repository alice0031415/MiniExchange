using MiniExchange.Application.Concurrency;
using MiniExchange.Infrastructure.Redis;
using StackExchange.Redis;

namespace MiniExchange.Tests;

public class RedisDistributedLockTests
{
    [Fact]
    public async Task OnlyOneOwner_CanAcquireSameLock()
    {
        await using var redis =
            await ConnectionMultiplexer.ConnectAsync(
                "localhost:6379");

        IDistributedLock distributedLock =
            new RedisDistributedLock(redis);

        var first =
            await distributedLock.TryAcquireAsync(
                "test-lock",
                TimeSpan.FromSeconds(10),
                CancellationToken.None);

        Assert.NotNull(first);

        var second =
            await distributedLock.TryAcquireAsync(
                "test-lock",
                TimeSpan.FromSeconds(10),
                CancellationToken.None);

        Assert.Null(second);

        await first.DisposeAsync();

        var third =
            await distributedLock.TryAcquireAsync(
                "test-lock",
                TimeSpan.FromSeconds(10),
                CancellationToken.None);

        Assert.NotNull(third);

        await third!.DisposeAsync();
    }
}