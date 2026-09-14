namespace MiniExchange.Domain;

public sealed class MatchingEngine
{
    public IReadOnlyList<TradeExecution> Match(
        OrderBook orderBook,
        Order incomingOrder)
    {
        var executions = new List<TradeExecution>();

        while (incomingOrder.RemainingQuantity > 0)
        {
            var oppositeOrder = FindMatch(
                orderBook,
                incomingOrder);

            if (oppositeOrder is null)
                break;

            var quantity = Math.Min(
                incomingOrder.RemainingQuantity,
                oppositeOrder.RemainingQuantity);

            var price = oppositeOrder.Price;

            var execution = incomingOrder.Side == OrderSide.Buy
                ? new TradeExecution(
                    incomingOrder.InstrumentId,
                    incomingOrder.Id,
                    oppositeOrder.Id,
                    price,
                    quantity)
                : new TradeExecution(
                    incomingOrder.InstrumentId,
                    oppositeOrder.Id,
                    incomingOrder.Id,
                    price,
                    quantity);

            incomingOrder.Execute(quantity);
            oppositeOrder.Execute(quantity);

            executions.Add(execution);

            if (oppositeOrder.Status == OrderStatus.Filled)
                orderBook.Remove(oppositeOrder);
        }

        if (incomingOrder.RemainingQuantity > 0)
            orderBook.Add(incomingOrder);

        return executions;
    }

    private static Order? FindMatch(
        OrderBook orderBook,
        Order incomingOrder)
    {
        if (incomingOrder.Side == OrderSide.Buy)
        {
            var bestAsk = orderBook.Asks.FirstOrDefault();

            if (bestAsk is null)
                return null;

            return incomingOrder.Price >= bestAsk.Price
                ? bestAsk
                : null;
        }

        var bestBid = orderBook.Bids.FirstOrDefault();

        if (bestBid is null)
            return null;

        return incomingOrder.Price <= bestBid.Price
            ? bestBid
            : null;
    }
}