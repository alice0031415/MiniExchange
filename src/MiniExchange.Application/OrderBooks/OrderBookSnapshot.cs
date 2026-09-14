namespace MiniExchange.Application.OrderBooks;

public sealed record OrderBookSnapshot(
    IReadOnlyList<OrderBookLevel> Bids,
    IReadOnlyList<OrderBookLevel> Asks);

public sealed record OrderBookLevel(
    decimal Price,
    decimal Quantity);