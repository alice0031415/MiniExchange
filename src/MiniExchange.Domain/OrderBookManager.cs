namespace MiniExchange.Domain;

public sealed class OrderBookManager
{
    private readonly Dictionary<Guid, OrderBook> _books = new();

    public bool TryGet(
        Guid instrumentId,
        out OrderBook? orderBook)
    {
        return _books.TryGetValue(
            instrumentId,
            out orderBook);
    }

    public OrderBook GetOrCreate(Guid instrumentId)
    {
        if (_books.TryGetValue(instrumentId, out var existing))
            return existing;

        var orderBook = new OrderBook(instrumentId);

        _books.Add(instrumentId, orderBook);

        return orderBook;
    }

    public OrderBook Load(
        Guid instrumentId,
        IEnumerable<Order> orders)
    {
        var orderBook = new OrderBook(instrumentId);

        foreach (var order in orders)
        {
            orderBook.Add(order);
        }

        _books[instrumentId] = orderBook;

        return orderBook;
    }
    public bool Remove(Guid instrumentId)
    {
        return _books.Remove(instrumentId);
    }
}