namespace MiniExchange.Domain;

public sealed class Instrument
{
    public Instrument(
        Guid id,
        string ticker,
        string name)
    {
        if (id == Guid.Empty)
            throw new ArgumentException(
                "Instrument id is required.",
                nameof(id));

        if (string.IsNullOrWhiteSpace(ticker))
            throw new ArgumentException(
                "Ticker is required.",
                nameof(ticker));

        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(
                "Name is required.",
                nameof(name));

        Id = id;
        Ticker = ticker;
        Name = name;
    }

    public Guid Id { get; }

    public string Ticker { get; }

    public string Name { get; }
}