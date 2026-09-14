using System.Text.Json;
using Confluent.Kafka;
using MiniExchange.Contracts.Events;

namespace MiniExchange.Infrastructure.Messaging;

public sealed class KafkaOrderEventPublisher
{
    private const string Topic = "orders";

    private readonly IProducer<string, string> _producer;

    public KafkaOrderEventPublisher(IProducer<string, string> producer)
    {
        _producer = producer;
    }

    public async Task PublishAsync(
        OrderSubmitted message,
        CancellationToken cancellationToken = default)
    {
        var value = JsonSerializer.Serialize(message);

        var kafkaMessage = new Message<string, string>
        {
            Key = message.InstrumentId.ToString(),
            Value = value
        };

        await _producer.ProduceAsync(
            Topic,
            kafkaMessage,
            cancellationToken);
    }
}