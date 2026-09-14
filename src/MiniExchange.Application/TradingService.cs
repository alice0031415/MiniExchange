using MiniExchange.Application.Instruments;
using MiniExchange.Application.Metrics;
using MiniExchange.Application.OrderBooks;
using MiniExchange.Application.Orders;
using MiniExchange.Application.Outbox;
using MiniExchange.Application.Trades;
using MiniExchange.Contracts.Events;
using MiniExchange.Domain;
using System.Diagnostics;
using System.Text.Json;

namespace MiniExchange.Application;

public sealed class TradingService : ITradingService
{
    private static readonly ActivitySource ActivitySource =
        new("MiniExchange.Trading");

    private readonly IInstrumentRepository _instruments;
    private readonly IOrderRepository _orders;
    private readonly ITradeRepository _trades;
    private readonly IOutboxRepository _outbox;
    private readonly IUnitOfWork _unitOfWork;
    private readonly MatchingEngine _matchingEngine;
    private readonly IOrderBookCache _orderBookCache;
    private readonly ITradingMetrics _metrics;

    public TradingService(
        IInstrumentRepository instruments,
        IOrderRepository orders,
        ITradeRepository trades,
        IOutboxRepository outbox,
        IUnitOfWork unitOfWork,
        MatchingEngine matchingEngine,
        IOrderBookCache orderBookCache,
        ITradingMetrics metrics)
    {
        _instruments = instruments;
        _orders = orders;
        _trades = trades;
        _outbox = outbox;
        _unitOfWork = unitOfWork;
        _matchingEngine = matchingEngine;
        _orderBookCache = orderBookCache;
        _metrics = metrics;
    }

    public async Task<Order> PlaceOrder(
        Guid instrumentId,
        OrderSide side,
        decimal price,
        decimal quantity,
        CancellationToken cancellationToken)
    {
        using var timer = _metrics.StartPlaceOrderTimer();

        using var activity =
            ActivitySource.StartActivity(
                "PlaceOrder",
                ActivityKind.Internal);

        activity?.SetTag(
            "miniexchange.instrument_id",
            instrumentId);

        activity?.SetTag(
            "miniexchange.order.side",
            side.ToString());

        activity?.SetTag(
            "miniexchange.order.price",
            price);

        activity?.SetTag(
            "miniexchange.order.quantity",
            quantity);

        try
        {
            if (!await _instruments.ExistsAsync(
                    instrumentId,
                    cancellationToken))
            {
                throw new KeyNotFoundException(
                    $"Instrument '{instrumentId}' was not found.");
            }

            await using var transaction =
                await _unitOfWork.BeginTransactionAsync(
                    cancellationToken);

            var existingOrders =
                await _orders.GetMatchingOrdersAsync(
                    instrumentId,
                    side,
                    price,
                    cancellationToken);

            var orderBook =
                new OrderBook(instrumentId);

            foreach (var order in existingOrders)
            {
                orderBook.Add(order);
            }

            var incomingOrder = Order.Create(
                instrumentId,
                side,
                price,
                quantity);

            var executions =
                _matchingEngine.Match(
                    orderBook,
                    incomingOrder);

            activity?.SetTag(
                "miniexchange.executions.count",
                executions.Count);

            var changedOrders =
                new Dictionary<Guid, Order>();

            if (incomingOrder.Status != OrderStatus.Active)
            {
                changedOrders[incomingOrder.Id] =
                    incomingOrder;
            }

            await _orders.AddAsync(
                incomingOrder,
                cancellationToken);

            _metrics.OrderCreated(
                incomingOrder.Side.ToString(),
                incomingOrder.Status.ToString());

            foreach (var execution in executions)
            {
                var buyOrder = FindOrder(
                    incomingOrder,
                    existingOrders,
                    execution.BuyOrderId);

                var sellOrder = FindOrder(
                    incomingOrder,
                    existingOrders,
                    execution.SellOrderId);

                if (buyOrder is not null &&
                    buyOrder.Id != incomingOrder.Id)
                {
                    changedOrders[buyOrder.Id] =
                        buyOrder;

                    await _orders.UpdateStateAsync(
                        buyOrder,
                        cancellationToken);
                }

                if (sellOrder is not null &&
                    sellOrder.Id != incomingOrder.Id)
                {
                    changedOrders[sellOrder.Id] =
                        sellOrder;

                    await _orders.UpdateStateAsync(
                        sellOrder,
                        cancellationToken);
                }

                var trade = new Trade(
                    execution.InstrumentId,
                    execution.BuyOrderId,
                    execution.SellOrderId,
                    execution.Price,
                    execution.Quantity);

                await _trades.AddAsync(
                    trade,
                    cancellationToken);

                var tradeExecuted =
                    new TradeExecuted(
                        trade.Id,
                        trade.InstrumentId,
                        trade.BuyOrderId,
                        trade.SellOrderId,
                        trade.Price,
                        trade.Quantity,
                        trade.ExecutedAt);

                var tradeEnvelope =
                    new EventEnvelope<TradeExecuted>(
                        Guid.NewGuid(),
                        nameof(TradeExecuted),
                        1,
                        trade.ExecutedAt,
                        tradeExecuted);

                await _outbox.AddAsync(
                    tradeEnvelope.EventId,
                    tradeEnvelope.EventType,
                    trade.InstrumentId.ToString(),
                    JsonSerializer.Serialize(tradeEnvelope),
                    tradeEnvelope.OccurredAt,
                    Activity.Current?.Id,
                    Activity.Current?.TraceStateString,
                    cancellationToken);

                _metrics.TradeExecuted();
            }

            var orderSubmitted =
                new OrderSubmitted(
                    incomingOrder.Id,
                    incomingOrder.InstrumentId,
                    incomingOrder.Side.ToString(),
                    incomingOrder.Price,
                    incomingOrder.Quantity,
                    incomingOrder.CreatedAt);

            var submittedEnvelope =
                new EventEnvelope<OrderSubmitted>(
                    Guid.NewGuid(),
                    nameof(OrderSubmitted),
                    1,
                    DateTime.UtcNow,
                    orderSubmitted);

            await _outbox.AddAsync(
                submittedEnvelope.EventId,
                submittedEnvelope.EventType,
                submittedEnvelope.EventId.ToString(),
                JsonSerializer.Serialize(submittedEnvelope),
                submittedEnvelope.OccurredAt,
                Activity.Current?.Id,
                Activity.Current?.TraceStateString,
                cancellationToken);

            foreach (var changedOrder in changedOrders.Values)
            {
                var stateChanged =
                    new OrderStateChanged(
                        changedOrder.Id,
                        changedOrder.InstrumentId,
                        changedOrder.Side.ToString(),
                        changedOrder.Price,
                        changedOrder.Quantity,
                        changedOrder.RemainingQuantity,
                        changedOrder.Status.ToString(),
                        DateTime.UtcNow);

                var stateChangedEnvelope =
                    new EventEnvelope<OrderStateChanged>(
                        Guid.NewGuid(),
                        nameof(OrderStateChanged),
                        1,
                        stateChanged.OccurredAt,
                        stateChanged);

                await _outbox.AddAsync(
                    stateChangedEnvelope.EventId,
                    stateChangedEnvelope.EventType,
                    stateChanged.InstrumentId.ToString(),
                    JsonSerializer.Serialize(stateChangedEnvelope),
                    stateChangedEnvelope.OccurredAt,
                    Activity.Current?.Id,
                    Activity.Current?.TraceStateString,
                    cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(
                cancellationToken);

            await transaction.CommitAsync(
                cancellationToken);

            try
            {
                await _orderBookCache.RemoveAsync(
                    instrumentId,
                    cancellationToken);
            }
            catch
            {
                // Redis is only a cache.
            }

            activity?.SetTag(
                "miniexchange.order_id",
                incomingOrder.Id);

            activity?.SetTag(
                "miniexchange.order.status",
                incomingOrder.Status.ToString());

            activity?.SetTag(
                "miniexchange.order.remaining_quantity",
                incomingOrder.RemainingQuantity);

            activity?.SetStatus(
                ActivityStatusCode.Ok);

            return incomingOrder;
        }
        catch (Exception ex)
        {
            activity?.SetTag(
                "error.type",
                ex.GetType().FullName);

            activity?.SetTag(
                "error.message",
                ex.Message);

            activity?.SetStatus(
                ActivityStatusCode.Error);

            throw;
        }
    }

    public async Task CancelOrder(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        await using var transaction =
            await _unitOfWork.BeginTransactionAsync(
                cancellationToken);

        var order = await _orders.GetByIdAsync(
            orderId,
            cancellationToken);

        if (order is null)
        {
            throw new KeyNotFoundException(
                $"Order '{orderId}' was not found.");
        }

        order.Cancel();

        await _orders.UpdateStateAsync(
            order,
            cancellationToken);

        var stateChanged =
            new OrderStateChanged(
                order.Id,
                order.InstrumentId,
                order.Side.ToString(),
                order.Price,
                order.Quantity,
                order.RemainingQuantity,
                order.Status.ToString(),
                DateTime.UtcNow);

        var envelope =
            new EventEnvelope<OrderStateChanged>(
                Guid.NewGuid(),
                nameof(OrderStateChanged),
                1,
                stateChanged.OccurredAt,
                stateChanged);

        await _outbox.AddAsync(
            envelope.EventId,
            envelope.EventType,
            stateChanged.InstrumentId.ToString(),
            JsonSerializer.Serialize(envelope),
            envelope.OccurredAt,
            Activity.Current?.Id,
            Activity.Current?.TraceStateString,
            cancellationToken);

        await _unitOfWork.SaveChangesAsync(
            cancellationToken);

        await transaction.CommitAsync(
            cancellationToken);

        _metrics.OrderCancelled();

        try
        {
            await _orderBookCache.RemoveAsync(
                order.InstrumentId,
                cancellationToken);
        }
        catch
        {
            // Redis is only a cache.
        }
    }

    private static Order? FindOrder(
        Order incomingOrder,
        IReadOnlyList<Order> existingOrders,
        Guid orderId)
    {
        if (incomingOrder.Id == orderId)
        {
            return incomingOrder;
        }

        return existingOrders.FirstOrDefault(
            x => x.Id == orderId);
    }
}