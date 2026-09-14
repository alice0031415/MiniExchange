using MiniExchange.Application.Trades;
using MiniExchange.Domain;
using MiniExchange.Infrastructure.Mappers;

namespace MiniExchange.Infrastructure.Repositories;

public sealed class TradeRepository : ITradeRepository
{
    private readonly MiniExchangeDbContext _db;

    public TradeRepository(MiniExchangeDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(
        Trade trade,
        CancellationToken cancellationToken)
    {
        var entity = TradeMapper.ToEntity(trade);

        await _db.Trades.AddAsync(
            entity,
            cancellationToken);
    }
}