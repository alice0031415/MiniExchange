using MiniExchange.Domain;
using MiniExchange.Infrastructure.Entities;

namespace MiniExchange.Infrastructure.Mappers;

public static class OrderMapper
{
    public static OrderEntity ToEntity(Order order)
    {
        return new OrderEntity
        {
            Id = order.Id,
            InstrumentId = order.InstrumentId,
            Side = (int)order.Side,
            Price = order.Price,
            Quantity = order.Quantity,
            RemainingQuantity = order.RemainingQuantity,
            Status = (int)order.Status,
            CreatedAt = order.CreatedAt
        };
    }

    public static Order ToDomain(OrderEntity entity)
    {
        return Order.Restore(
            entity.Id,
            entity.InstrumentId,
            (OrderSide)entity.Side,
            entity.Price,
            entity.Quantity,
            entity.RemainingQuantity,
            (OrderStatus)entity.Status,
            entity.CreatedAt);
    }
}