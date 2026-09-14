namespace MiniExchange.Application.Metrics;

public interface ITradingMetrics
{
    void OrderCreated(
        string side,
        string status);

    void OrderCancelled();

    void TradeExecuted();

    IDisposable StartPlaceOrderTimer();
}