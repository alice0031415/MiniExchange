namespace MiniExchange.Infrastructure.Entities;

public class InstrumentEntity
{
    public Guid Id { get; set; }

    public string Ticker { get; set; } = null!;

    public string Name { get; set; } = null!;
}