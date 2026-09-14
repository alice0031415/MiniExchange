namespace MiniExchange.KafkaConsumer;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } =
        "localhost:9092";

    public string GroupId { get; set; } =
        "orders-consumer";

    public string Topic { get; set; } =
        "orders";

    public string DeadLetterTopic { get; set; } =
        "orders.dlq";
}