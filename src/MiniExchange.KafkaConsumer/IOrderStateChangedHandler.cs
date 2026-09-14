using MiniExchange.Contracts.Events;

namespace MiniExchange.KafkaConsumer;

public interface IOrderStateChangedHandler
{
    Task HandleAsync(
        EventEnvelope<OrderStateChanged> envelope,
        CancellationToken cancellationToken);
}