namespace MiniExchange.Domain;

public class Order
{
    private Order()
    {
    }

    private Order(
        Guid id,
        Guid instrumentId,
        OrderSide side,
        decimal price,
        decimal quantity,
        DateTime createdAt)
    {
        Id = id;
        InstrumentId = instrumentId;
        Side = side;
        Price = price;
        Quantity = quantity;
        RemainingQuantity = quantity;
        Status = OrderStatus.Active;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }

    public Guid InstrumentId { get; private set; }

    public OrderSide Side { get; private set; }

    public decimal Price { get; private set; }

    public decimal Quantity { get; private set; }

    public decimal RemainingQuantity { get; private set; }

    public OrderStatus Status { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public static Order Create(
        Guid instrumentId,
        OrderSide side,
        decimal price,
        decimal quantity)
    {
        if (instrumentId == Guid.Empty)
            throw new ArgumentException(
                "Instrument id is required.",
                nameof(instrumentId));

        if (price <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(price));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(quantity));

        return new Order(
            Guid.NewGuid(),
            instrumentId,
            side,
            price,
            quantity,
            DateTime.UtcNow);
    }

    public void Cancel()
    {
        if (Status != OrderStatus.Active &&
            Status != OrderStatus.PartiallyFilled)
        {
            throw new InvalidOperationException(
                $"Order cannot be cancelled from status {Status}.");
        }

        Status = OrderStatus.Cancelled;
    }

    public void Execute(decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (quantity > RemainingQuantity)
        {
            throw new InvalidOperationException(
                "Execution quantity cannot exceed remaining quantity.");
        }

        RemainingQuantity -= quantity;

        Status = RemainingQuantity == 0
            ? OrderStatus.Filled
            : OrderStatus.PartiallyFilled;

    }
    public static Order Restore(
        Guid id,
        Guid instrumentId,
        OrderSide side,
        decimal price,
        decimal quantity,
        decimal remainingQuantity,
        OrderStatus status,
        DateTime createdAt)
    {
        return new Order
        {
            Id = id,
            InstrumentId = instrumentId,
            Side = side,
            Price = price,
            Quantity = quantity,
            RemainingQuantity = remainingQuantity,
            Status = status,
            CreatedAt = createdAt
        };
    }
}