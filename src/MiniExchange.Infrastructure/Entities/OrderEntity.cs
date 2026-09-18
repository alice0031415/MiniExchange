namespace MiniExchange.Infrastructure.Entities;

public class OrderEntity
{
    public Guid Id { get; set; }

    public Guid InstrumentId { get; set; }

    public int Side { get; set; }

    public decimal Price { get; set; }

    public decimal Quantity { get; set; }

    public decimal RemainingQuantity { get; set; }

    public int Status { get; set; }

    public DateTime CreatedAt { get; set; }
}