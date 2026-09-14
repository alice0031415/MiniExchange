namespace MiniExchange.Domain;

public sealed record TradeExecution(
    Guid InstrumentId,
    Guid BuyOrderId,
    Guid SellOrderId,
    decimal Price,
    decimal Quantity);