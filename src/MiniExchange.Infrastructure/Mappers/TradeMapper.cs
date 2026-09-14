using MiniExchange.Domain;
using MiniExchange.Infrastructure.Entities;

namespace MiniExchange.Infrastructure.Mappers;

public static class TradeMapper
{
    public static TradeEntity ToEntity(
        Trade trade)
    {
        return new TradeEntity
        {
            Id = trade.Id,
            InstrumentId = trade.InstrumentId,
            BuyOrderId = trade.BuyOrderId,
            SellOrderId = trade.SellOrderId,
            Price = trade.Price,
            Quantity = trade.Quantity,
            ExecutedAt = trade.ExecutedAt
        };
    }
}