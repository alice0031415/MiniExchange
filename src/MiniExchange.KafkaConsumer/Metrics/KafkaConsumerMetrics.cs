using Prometheus;

namespace MiniExchange.KafkaConsumer.Metrics;

public static class KafkaConsumerMetrics
{
    public static readonly Counter MessagesProcessed =
        global::Prometheus.Metrics.CreateCounter(
            "miniexchange_kafka_messages_processed_total",
            "Total number of successfully processed Kafka messages.");

    public static readonly Counter MessagesRetried =
        global::Prometheus.Metrics.CreateCounter(
            "miniexchange_kafka_messages_retried_total",
            "Total number of Kafka message retry attempts.");

    public static readonly Counter MessagesDeadLettered =
        global::Prometheus.Metrics.CreateCounter(
            "miniexchange_kafka_messages_dead_lettered_total",
            "Total number of messages published to the Kafka DLQ.");
}