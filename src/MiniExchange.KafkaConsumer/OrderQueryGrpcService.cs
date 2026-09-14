using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using MiniExchange.Contracts.Grpc;
using MiniExchange.KafkaConsumer.Data;

namespace MiniExchange.KafkaConsumer;

public sealed class OrderQueryGrpcService
    : OrderQuery.OrderQueryBase
{
    private readonly ConsumerDbContext _db;

    public OrderQueryGrpcService(
        ConsumerDbContext db)
    {
        _db = db;
    }

    public override async Task<GetOrdersResponse> GetOrders(
        GetOrdersRequest request,
        ServerCallContext context)
    {
        Guid? instrumentId = null;

        if (!string.IsNullOrWhiteSpace(
                request.InstrumentId))
        {
            if (!Guid.TryParse(
                    request.InstrumentId,
                    out var parsedId))
            {
                throw new RpcException(
                    new Status(
                        StatusCode.InvalidArgument,
                        "Invalid instrument_id."));
            }

            instrumentId = parsedId;
        }

        var query = _db.OrderViews
            .AsNoTracking()
            .AsQueryable();

        if (instrumentId.HasValue)
        {
            query = query.Where(
                x => x.InstrumentId == instrumentId.Value);
        }

        var orders = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(context.CancellationToken);

        var response =
            new GetOrdersResponse();

        response.Orders.AddRange(
            orders.Select(x =>
                new OrderDto
                {
                    OrderId = x.OrderId.ToString(),
                    InstrumentId = x.InstrumentId.ToString(),
                    Side = x.Side,
                    Price = (double)x.Price,
                    Quantity = (double)x.Quantity,
                    RemainingQuantity =
                        (double)x.RemainingQuantity,
                    Status = x.Status,
                    CreatedAt =
                        x.CreatedAt.ToString("O")
                }));

        return response;
    }

    public override async Task<GetTradesResponse> GetTrades(
        GetTradesRequest request,
        ServerCallContext context)
    {
        Guid? instrumentId = null;

        if (!string.IsNullOrWhiteSpace(
                request.InstrumentId))
        {
            if (!Guid.TryParse(
                    request.InstrumentId,
                    out var parsedId))
            {
                throw new RpcException(
                    new Status(
                        StatusCode.InvalidArgument,
                        "Invalid instrument_id."));
            }

            instrumentId = parsedId;
        }

        var query = _db.TradeViews
            .AsNoTracking()
            .AsQueryable();

        if (instrumentId.HasValue)
        {
            query = query.Where(
                x => x.InstrumentId == instrumentId.Value);
        }

        var trades = await query
            .OrderByDescending(x => x.ExecutedAt)
            .ToListAsync(context.CancellationToken);

        var response =
            new GetTradesResponse();

        response.Trades.AddRange(
            trades.Select(x =>
                new TradeDto
                {
                    TradeId = x.TradeId.ToString(),
                    InstrumentId = x.InstrumentId.ToString(),
                    BuyOrderId = x.BuyOrderId.ToString(),
                    SellOrderId = x.SellOrderId.ToString(),
                    Price = (double)x.Price,
                    Quantity = (double)x.Quantity,
                    ExecutedAt =
                        x.ExecutedAt.ToString("O")
                }));

        return response;
    }
}