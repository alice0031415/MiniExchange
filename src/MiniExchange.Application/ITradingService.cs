using MiniExchange.Domain;

namespace MiniExchange.Application;

public interface ITradingService
{
    Task<Order> PlaceOrder(
        Guid instrumentId,
        OrderSide side,
        decimal price,
        decimal quantity,
        CancellationToken cancellationToken);

    Task CancelOrder(
        Guid orderId,
        CancellationToken cancellationToken);
}