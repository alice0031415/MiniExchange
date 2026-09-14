namespace MiniExchange.Application;

public interface IUnitOfWork
{
    Task SaveChangesAsync(
        CancellationToken cancellationToken);

    Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken);
}