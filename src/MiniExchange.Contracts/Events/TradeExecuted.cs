namespace MiniExchange.Contracts.Events;

public sealed record TradeExecuted(
    Guid TradeId,
    Guid InstrumentId,
    Guid BuyOrderId,
    Guid SellOrderId,
    decimal Price,
    decimal Quantity,
    DateTime ExecutedAt);