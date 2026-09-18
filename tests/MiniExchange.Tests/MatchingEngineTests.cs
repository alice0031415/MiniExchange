using MiniExchange.Domain;

namespace MiniExchange.Tests;

public class MatchingEngineTests
{

    [Fact]
    public void OrderBook_CannotContainOrdersOfAnotherInstrument()
    {
        var sberId = Guid.NewGuid();
        var gazpId = Guid.NewGuid();

        var orderBook = new OrderBook(sberId);

        var gazpOrder = Order.Create(
            gazpId,
            OrderSide.Buy,
            250m,
            100m);

        Assert.Throws<InvalidOperationException>(
            () => orderBook.Add(gazpOrder));
    }
    [Fact]
    public void OrderBook_Bids_AreOrderedByBestPriceFirst()
    {
        var instrumentId = Guid.NewGuid();
        var orderBook = new OrderBook(instrumentId);

        var low = Order.Create(
            instrumentId,
            OrderSide.Buy,
            248m,
            100m);

        var high = Order.Create(
            instrumentId,
            OrderSide.Buy,
            252m,
            100m);

        var middle = Order.Create(
            instrumentId,
            OrderSide.Buy,
            250m,
            100m);

        orderBook.Add(low);
        orderBook.Add(high);
        orderBook.Add(middle);

        var orders = orderBook.Bids.ToArray();

        Assert.Equal(high.Id, orders[0].Id);
        Assert.Equal(middle.Id, orders[1].Id);
        Assert.Equal(low.Id, orders[2].Id);
    }
    [Fact]
    public void OrderBook_Asks_AreOrderedByBestPriceFirst()
    {
        var instrumentId = Guid.NewGuid();
        var orderBook = new OrderBook(instrumentId);

        var expensive = Order.Create(
            instrumentId,
            OrderSide.Sell,
            252m,
            100m);

        var cheap = Order.Create(
            instrumentId,
            OrderSide.Sell,
            248m,
            100m);

        var middle = Order.Create(
            instrumentId,
            OrderSide.Sell,
            250m,
            100m);

        orderBook.Add(expensive);
        orderBook.Add(cheap);
        orderBook.Add(middle);

        var orders = orderBook.Asks.ToArray();

        Assert.Equal(cheap.Id, orders[0].Id);
        Assert.Equal(middle.Id, orders[1].Id);
        Assert.Equal(expensive.Id, orders[2].Id);
    }
    [Fact]
    public void OrderBook_SamePrice_UsesTimePriority()
    {
        var instrumentId = Guid.NewGuid();

        var first = Order.Create(
            instrumentId,
            OrderSide.Buy,
            250m,
            100m);

        Thread.Sleep(2);

        var second = Order.Create(
            instrumentId,
            OrderSide.Buy,
            250m,
            200m);

        var orderBook = new OrderBook(instrumentId);

        orderBook.Add(second);
        orderBook.Add(first);

        var orders = orderBook.Bids.ToArray();

        Assert.Equal(first.Id, orders[0].Id);
        Assert.Equal(second.Id, orders[1].Id);
    }
}