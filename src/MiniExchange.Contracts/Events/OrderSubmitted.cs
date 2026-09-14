namespace MiniExchange.Contracts.Events;

public sealed record OrderSubmitted(
    Guid OrderId,
    Guid InstrumentId,
    string Side,
    decimal Price,
    decimal Quantity,
    DateTime CreatedAt);