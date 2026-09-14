namespace MiniExchange.Application.OrderBooks;

public interface IOrderBookService
{
    Task<OrderBookSnapshot> GetAsync(
        Guid instrumentId,
        CancellationToken cancellationToken);
}