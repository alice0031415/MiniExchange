namespace MiniExchange.Contracts.Events;

public sealed record OrderStateChanged(
    Guid OrderId,
    Guid InstrumentId,
    string Side,
    decimal Price,
    decimal Quantity,
    decimal RemainingQuantity,
    string Status,
    DateTime OccurredAt);