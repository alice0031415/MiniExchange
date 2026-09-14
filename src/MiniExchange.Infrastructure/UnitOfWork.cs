using Microsoft.EntityFrameworkCore;
using MiniExchange.Application;

namespace MiniExchange.Infrastructure;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MiniExchangeDbContext _db;

    public UnitOfWork(MiniExchangeDbContext db)
    {
        _db = db;
    }

    public Task SaveChangesAsync(
        CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(
        CancellationToken cancellationToken)
    {
        var transaction =
            await _db.Database.BeginTransactionAsync(
                cancellationToken);

        return new UnitOfWorkTransaction(transaction);
    }
}