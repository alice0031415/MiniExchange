using Microsoft.EntityFrameworkCore;
using MiniExchange.Application.Orders;
using MiniExchange.Domain;
using MiniExchange.Infrastructure.Mappers;

namespace MiniExchange.Infrastructure.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly MiniExchangeDbContext _db;

    public OrderRepository(MiniExchangeDbContext db)
    {
        _db = db;
    }

    public async Task<Order?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var entity = await _db.Orders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x => x.Id == id,
                cancellationToken);

        return entity is null
            ? null
            : OrderMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<Order>> GetOrdersAsync(
        Guid? instrumentId,
        CancellationToken cancellationToken)
    {
        var query = _db.Orders
            .AsNoTracking()
            .AsQueryable();

        if (instrumentId.HasValue)
        {
            query = query.Where(
                x => x.InstrumentId == instrumentId.Value);
        }

        var entities = await query
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return entities
            .Select(OrderMapper.ToDomain)
            .ToList();
    }

    public async Task<IReadOnlyList<Order>> GetMatchingOrdersAsync(
        Guid instrumentId,
        OrderSide side,
        decimal price,
        CancellationToken cancellationToken)
    {
        var oppositeSide =
            side == OrderSide.Buy
                ? OrderSide.Sell
                : OrderSide.Buy;

        var active = (int)OrderStatus.Active;
        var partiallyFilled = (int)OrderStatus.PartiallyFilled;

        var entities = side == OrderSide.Buy
            ? await _db.Orders
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM "Orders"
                    WHERE "InstrumentId" = {instrumentId}
                      AND "Side" = {(int)oppositeSide}
                      AND "Status" IN ({active}, {partiallyFilled})
                      AND "Price" <= {price}
                    ORDER BY
                        "Price" ASC,
                        "CreatedAt" ASC,
                        "Id" ASC
                    FOR UPDATE
                    """)
                .ToListAsync(cancellationToken)
            : await _db.Orders
                .FromSqlInterpolated($"""
                    SELECT *
                    FROM "Orders"
                    WHERE "InstrumentId" = {instrumentId}
                      AND "Side" = {(int)oppositeSide}
                      AND "Status" IN ({active}, {partiallyFilled})
                      AND "Price" >= {price}
                    ORDER BY
                        "Price" DESC,
                        "CreatedAt" ASC,
                        "Id" ASC
                    FOR UPDATE
                    """)
                .ToListAsync(cancellationToken);

        return entities
            .Select(OrderMapper.ToDomain)
            .ToList();
    }

    public async Task AddAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        var entity = OrderMapper.ToEntity(order);

        await _db.Orders.AddAsync(
            entity,
            cancellationToken);
    }

    public async Task UpdateStateAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        var entity = await _db.Orders
            .SingleAsync(
                x => x.Id == order.Id,
                cancellationToken);

        entity.RemainingQuantity =
            order.RemainingQuantity;

        entity.Status =
            (int)order.Status;
    }

    public async Task<IReadOnlyList<Order>> GetActiveOrdersAsync(
    Guid instrumentId,
    CancellationToken cancellationToken)
    {
        var active = (int)OrderStatus.Active;
        var partiallyFilled = (int)OrderStatus.PartiallyFilled;

        var entities = await _db.Orders
            .AsNoTracking()
            .Where(x =>
                x.InstrumentId == instrumentId &&
                (x.Status == active ||
                 x.Status == partiallyFilled))
            .OrderBy(x => x.Side)
            .ThenBy(x => x.Price)
            .ThenBy(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities
            .Select(OrderMapper.ToDomain)
            .ToList();
    }

    public async Task<Order?> GetByIdForUpdateAsync(
    Guid id,
    CancellationToken cancellationToken)
    {
        var entity = await _db.Orders
            .FromSqlInterpolated($"""
            SELECT *
            FROM "Orders"
            WHERE "Id" = {id}
            FOR UPDATE
            """)
            .AsNoTracking()
            .SingleOrDefaultAsync(cancellationToken);

        return entity is null
            ? null
            : OrderMapper.ToDomain(entity);
    }
}