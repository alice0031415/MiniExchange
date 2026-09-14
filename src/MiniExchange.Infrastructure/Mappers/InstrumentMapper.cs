using MiniExchange.Domain;
using MiniExchange.Infrastructure.Entities;

namespace MiniExchange.Infrastructure.Mappers;

public static class InstrumentMapper
{
    public static InstrumentEntity ToEntity(
        Instrument instrument)
    {
        return new InstrumentEntity
        {
            Id = instrument.Id,
            Ticker = instrument.Ticker,
            Name = instrument.Name
        };
    }

    public static Instrument ToDomain(
        InstrumentEntity entity)
    {
        return new Instrument(
            entity.Id,
            entity.Ticker,
            entity.Name);
    }
}