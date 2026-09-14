using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniExchange.Infrastructure;

namespace MiniExchange.Api.Controllers;

[ApiController]
[Route("trades")]
public class TradesController : ControllerBase
{
    private readonly MiniExchangeDbContext _db;

    public TradesController(
        MiniExchangeDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? instrumentId,
        CancellationToken cancellationToken)
    {
        var query = _db.Trades
            .AsNoTracking()
            .AsQueryable();

        if (instrumentId.HasValue)
        {
            query = query.Where(
                x => x.InstrumentId == instrumentId.Value);
        }

        var trades = await query
            .OrderByDescending(x => x.ExecutedAt)
            .ToListAsync(cancellationToken);

        return Ok(trades);
    }
}