using Microsoft.EntityFrameworkCore;
using MiniExchange.Application.Instruments;

namespace MiniExchange.Infrastructure.Repositories;

public sealed class InstrumentRepository
    : IInstrumentRepository
{
    private readonly MiniExchangeDbContext _db;

    public InstrumentRepository(
        MiniExchangeDbContext db)
    {
        _db = db;
    }

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        return _db.Instruments
            .AnyAsync(
                x => x.Id == id,
                cancellationToken);
    }
}