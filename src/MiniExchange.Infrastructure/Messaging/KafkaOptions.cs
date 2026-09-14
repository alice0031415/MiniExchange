namespace MiniExchange.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public string BootstrapServers { get; set; } = "localhost:9092";

    public string OrdersTopic { get; set; } = "orders";

    public int OutboxBatchSize { get; set; } = 100;

    public int OutboxPollIntervalMs { get; set; } = 1000;
}