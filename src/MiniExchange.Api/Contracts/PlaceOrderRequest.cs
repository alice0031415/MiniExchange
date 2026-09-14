using MiniExchange.Domain;

namespace MiniExchange.Api.Contracts;

public sealed record PlaceOrderRequest(
    Guid InstrumentId,
    OrderSide Side,
    decimal Price,
    decimal Quantity);