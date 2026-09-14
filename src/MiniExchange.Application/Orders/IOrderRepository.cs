using MiniExchange.Domain;

namespace MiniExchange.Application.Orders;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetOrdersAsync(
        Guid? instrumentId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetMatchingOrdersAsync(
        Guid instrumentId,
        OrderSide side,
        decimal price,
        CancellationToken cancellationToken);

    Task AddAsync(
        Order order,
        CancellationToken cancellationToken);

    Task UpdateStateAsync(
        Order order,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Order>> GetActiveOrdersAsync(
    Guid instrumentId,
    CancellationToken cancellationToken);
}