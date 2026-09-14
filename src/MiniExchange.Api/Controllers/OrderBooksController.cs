using Microsoft.AspNetCore.Mvc;
using MiniExchange.Application.OrderBooks;

namespace MiniExchange.Api.Controllers;

[ApiController]
[Route("orderbooks")]
public class OrderBooksController : ControllerBase
{
    private readonly IOrderBookService _service;

    public OrderBooksController(
        IOrderBookService service)
    {
        _service = service;
    }

    [HttpGet("{instrumentId:guid}")]
    public async Task<ActionResult<OrderBookSnapshot>> Get(
        Guid instrumentId,
        CancellationToken cancellationToken)
    {
        var snapshot = await _service.GetAsync(
            instrumentId,
            cancellationToken);

        return Ok(snapshot);
    }
}