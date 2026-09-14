namespace MiniExchange.Domain;

public sealed class OrderBook
{
    private readonly SortedSet<Order> _bids;
    private readonly SortedSet<Order> _asks;

    public OrderBook(Guid instrumentId)
    {
        if (instrumentId == Guid.Empty)
        {
            throw new ArgumentException(
                "Instrument id is required.",
                nameof(instrumentId));
        }

        InstrumentId = instrumentId;

        _bids = new SortedSet<Order>(
            Comparer<Order>.Create(CompareBids));

        _asks = new SortedSet<Order>(
            Comparer<Order>.Create(CompareAsks));
    }

    public Guid InstrumentId { get; }

    public IReadOnlyCollection<Order> Bids => _bids;

    public IReadOnlyCollection<Order> Asks => _asks;

    public void Add(Order order)
    {
        EnsureInstrument(order);

        if (order.Status != OrderStatus.Active &&
            order.Status != OrderStatus.PartiallyFilled)
        {
            throw new InvalidOperationException(
                "Only active orders can be added to the order book.");
        }

        if (order.Side == OrderSide.Buy)
            _bids.Add(order);
        else
            _asks.Add(order);
    }

    public void Remove(Order order)
    {
        EnsureInstrument(order);

        if (order.Side == OrderSide.Buy)
            _bids.Remove(order);
        else
            _asks.Remove(order);
    }

    private void EnsureInstrument(Order order)
    {
        if (order.InstrumentId != InstrumentId)
        {
            throw new InvalidOperationException(
                $"Order '{order.Id}' belongs to instrument " +
                $"'{order.InstrumentId}', but this order book " +
                $"belongs to '{InstrumentId}'.");
        }
    }

    private static int CompareBids(Order x, Order y)
    {
        var result = y.Price.CompareTo(x.Price);

        if (result != 0)
            return result;

        result = x.CreatedAt.CompareTo(y.CreatedAt);

        if (result != 0)
            return result;

        return x.Id.CompareTo(y.Id);
    }

    private static int CompareAsks(Order x, Order y)
    {
        var result = x.Price.CompareTo(y.Price);

        if (result != 0)
            return result;

        result = x.CreatedAt.CompareTo(y.CreatedAt);

        if (result != 0)
            return result;

        return x.Id.CompareTo(y.Id);
    }
}