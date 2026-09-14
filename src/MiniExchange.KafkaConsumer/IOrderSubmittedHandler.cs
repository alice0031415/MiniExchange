using MiniExchange.Contracts.Events;

namespace MiniExchange.KafkaConsumer;

public interface IOrderSubmittedHandler
{
    Task HandleAsync(
        EventEnvelope<OrderSubmitted> envelope,
        CancellationToken cancellationToken);
}