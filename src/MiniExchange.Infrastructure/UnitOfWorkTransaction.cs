using Microsoft.EntityFrameworkCore.Storage;
using MiniExchange.Application;

namespace MiniExchange.Infrastructure;

public sealed class UnitOfWorkTransaction
    : IUnitOfWorkTransaction
{
    private readonly IDbContextTransaction _transaction;

    public UnitOfWorkTransaction(
        IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }

    public Task CommitAsync(
        CancellationToken cancellationToken)
    {
        return _transaction.CommitAsync(
            cancellationToken);
    }

    public Task RollbackAsync(
        CancellationToken cancellationToken)
    {
        return _transaction.RollbackAsync(
            cancellationToken);
    }

    public ValueTask DisposeAsync()
    {
        return _transaction.DisposeAsync();
    }
}