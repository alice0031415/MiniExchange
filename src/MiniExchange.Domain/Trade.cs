namespace MiniExchange.Domain;

public class Trade
{
    private Trade()
    {
    }

    public Trade(
        Guid instrumentId,
        Guid buyOrderId,
        Guid sellOrderId,
        decimal price,
        decimal quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity));

        if (price <= 0)
            throw new ArgumentOutOfRangeException(nameof(price));

        Id = Guid.NewGuid();
        InstrumentId = instrumentId;
        BuyOrderId = buyOrderId;
        SellOrderId = sellOrderId;
        Price = price;
        Quantity = quantity;
        ExecutedAt = DateTime.UtcNow;
    }

    public Guid Id { get; }

    public Guid InstrumentId { get; }

    public Guid BuyOrderId { get; }

    public Guid SellOrderId { get; }

    public decimal Price { get; }

    public decimal Quantity { get; }

    public DateTime ExecutedAt { get; }
}