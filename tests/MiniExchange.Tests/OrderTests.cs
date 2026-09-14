using MiniExchange.Domain;

namespace MiniExchange.Tests;

public class OrderTests
{
    [Fact]
    public void Create_WithValidParameters_CreatesActiveOrder()
    {
        var instrumentId = Guid.NewGuid();

        var order = Order.Create(
            instrumentId,
            OrderSide.Buy,
            250m,
            100m);

        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(instrumentId, order.InstrumentId);
        Assert.Equal(OrderSide.Buy, order.Side);
        Assert.Equal(250m, order.Price);
        Assert.Equal(100m, order.Quantity);
        Assert.Equal(100m, order.RemainingQuantity);
        Assert.Equal(OrderStatus.Active, order.Status);
    }

    [Fact]
    public void Create_WithZeroPrice_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Order.Create(
                Guid.NewGuid(),
                OrderSide.Buy,
                0m,
                100m));

        Assert.Equal("price", exception.ParamName);
    }

    [Fact]
    public void Create_WithNegativeQuantity_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => Order.Create(
                Guid.NewGuid(),
                OrderSide.Buy,
                250m,
                -1m));

        Assert.Equal("quantity", exception.ParamName);
    }

    [Fact]
    public void Cancel_ActiveOrder_ChangesStatusToCancelled()
    {
        var order = Order.Create(
            Guid.NewGuid(),
            OrderSide.Buy,
            250m,
            100m);

        order.Cancel();

        Assert.Equal(
            OrderStatus.Cancelled,
            order.Status);
    }

    [Fact]
    public void Cancel_CancelledOrder_Throws()
    {
        var order = Order.Create(
            Guid.NewGuid(),
            OrderSide.Buy,
            250m,
            100m);

        order.Cancel();

        Assert.Throws<InvalidOperationException>(
            () => order.Cancel());
    }
    [Fact]
    public void Execute_CannotExceedRemainingQuantity()
    {
        var order = Order.Create(
            Guid.NewGuid(),
            OrderSide.Sell,
            250m,
            100m);

        order.Execute(60m);

        Assert.Throws<InvalidOperationException>(
            () => order.Execute(50m));
    }
    [Fact]
    public void PartiallyFilledOrder_CanBeCancelled()
    {
        var order = Order.Create(
            Guid.NewGuid(),
            OrderSide.Buy,
            250m,
            100m);

        order.Execute(40m);

        Assert.Equal(
            OrderStatus.PartiallyFilled,
            order.Status);

        order.Cancel();

        Assert.Equal(
            OrderStatus.Cancelled,
            order.Status);
    }
    [Fact]
    public void FilledOrder_CannotBeCancelled()
    {
        var order = Order.Create(
            Guid.NewGuid(),
            OrderSide.Buy,
            250m,
            100m);

        order.Execute(100m);

        Assert.Equal(
            OrderStatus.Filled,
            order.Status);

        Assert.Throws<InvalidOperationException>(
            () => order.Cancel());
    }
}