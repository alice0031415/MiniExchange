using MiniExchange.Contracts.Events;

namespace MiniExchange.KafkaConsumer;

public interface ITradeExecutedHandler
{
    Task HandleAsync(
        EventEnvelope<TradeExecuted> envelope,
        CancellationToken cancellationToken);
}