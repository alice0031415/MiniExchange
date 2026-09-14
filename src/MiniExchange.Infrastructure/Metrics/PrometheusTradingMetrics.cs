using MiniExchange.Application.Metrics;
using Prometheus;

namespace MiniExchange.Infrastructure.Metrics;

public sealed class PrometheusTradingMetrics : ITradingMetrics
{
    private static readonly Counter OrdersCreated =
        Prometheus.Metrics.CreateCounter(
            "miniexchange_orders_created_total",
            "Total number of orders created.",
            new CounterConfiguration
            {
                LabelNames = ["side", "status"]
            });

    private static readonly Counter OrdersCancelled =
        Prometheus.Metrics.CreateCounter(
            "miniexchange_orders_cancelled_total",
            "Total number of orders cancelled.");

    private static readonly Counter TradesExecuted =
        Prometheus.Metrics.CreateCounter(
            "miniexchange_trades_executed_total",
            "Total number of executed trades.");

    private static readonly Histogram PlaceOrderDuration =
        Prometheus.Metrics.CreateHistogram(
            "miniexchange_place_order_duration_seconds",
            "Duration of PlaceOrder operation.");

    public void OrderCreated(
        string side,
        string status)
    {
        OrdersCreated
            .WithLabels(side, status)
            .Inc();
    }

    public void OrderCancelled()
    {
        OrdersCancelled.Inc();
    }

    public void TradeExecuted()
    {
        TradesExecuted.Inc();
    }

    public IDisposable StartPlaceOrderTimer()
    {
        return PlaceOrderDuration.NewTimer();
    }
}