namespace MiniExchange.Application;

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(
        CancellationToken cancellationToken);

    Task RollbackAsync(
        CancellationToken cancellationToken);
}