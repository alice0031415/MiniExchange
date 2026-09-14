using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MiniExchange.Api.Contracts;
using MiniExchange.Infrastructure;
using MiniExchange.Infrastructure.Entities;

namespace MiniExchange.Api.Controllers;

[ApiController]
[Route("instruments")]
public class InstrumentsController : ControllerBase
{
    private readonly MiniExchangeDbContext _db;

    public InstrumentsController(MiniExchangeDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<List<InstrumentEntity>>> GetAll(
        CancellationToken cancellationToken)
    {
        return await _db.Instruments
            .AsNoTracking()
            .OrderBy(x => x.Ticker)
            .ToListAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<ActionResult<InstrumentEntity>> Create(
        CreateInstrumentRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Ticker))
            return BadRequest("Ticker is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest("Name is required.");

        var ticker = request.Ticker.Trim().ToUpperInvariant();

        var exists = await _db.Instruments
            .AnyAsync(
                x => x.Ticker == ticker,
                cancellationToken);

        if (exists)
            return Conflict(
                $"Instrument '{ticker}' already exists.");

        var instrument = new InstrumentEntity
        {
            Id = Guid.NewGuid(),
            Ticker = ticker,
            Name = request.Name.Trim()
        };

        _db.Instruments.Add(instrument);

        await _db.SaveChangesAsync(cancellationToken);

        return Created(
            $"/instruments/{instrument.Id}",
            instrument);
    }
}