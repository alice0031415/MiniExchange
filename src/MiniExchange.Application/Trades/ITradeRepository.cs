using MiniExchange.Domain;

namespace MiniExchange.Application.Trades;

public interface ITradeRepository
{
    Task AddAsync(
        Trade trade,
        CancellationToken cancellationToken);
}