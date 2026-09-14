namespace MiniExchange.KafkaConsumer.Data;

public sealed class TradeViewEntity
{
    public Guid TradeId { get; set; }

    public Guid InstrumentId { get; set; }

    public Guid BuyOrderId { get; set; }

    public Guid SellOrderId { get; set; }

    public decimal Price { get; set; }

    public decimal Quantity { get; set; }

    public DateTime ExecutedAt { get; set; }
}