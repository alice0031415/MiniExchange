namespace MiniExchange.Application.Concurrency;

public interface IDistributedLock
{
    Task<IAsyncDisposable?> TryAcquireAsync(
        string key,
        TimeSpan expiration,
        CancellationToken cancellationToken);
}