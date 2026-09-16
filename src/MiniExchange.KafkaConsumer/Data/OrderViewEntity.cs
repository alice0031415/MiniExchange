namespace MiniExchange.KafkaConsumer.Data;

public sealed class OrderViewEntity
{
    public Guid OrderId { get; set; }

    public Guid InstrumentId { get; set; }

    public string Side { get; set; } = null!;

    public decimal Price { get; set; }

    public decimal Quantity { get; set; }

    public decimal RemainingQuantity { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
}