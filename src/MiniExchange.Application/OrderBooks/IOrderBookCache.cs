namespace MiniExchange.Application.OrderBooks;

public interface IOrderBookCache
{
    Task SetAsync(
        Guid instrumentId,
        OrderBookSnapshot snapshot,
        CancellationToken cancellationToken);

    Task<OrderBookSnapshot?> GetAsync(
        Guid instrumentId,
        CancellationToken cancellationToken);

    Task RemoveAsync(
        Guid instrumentId,
        CancellationToken cancellationToken);
}