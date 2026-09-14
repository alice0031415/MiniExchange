using Microsoft.AspNetCore.Mvc;
using MiniExchange.Contracts.Grpc;
using MiniExchange.ReadApi.Services;

namespace MiniExchange.ReadApi.Controllers;

[ApiController]
[Route("orders")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrderQueryClient _client;

    public OrdersController(
        OrderQueryClient client)
    {
        _client = client;
    }

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] Guid? instrumentId,
        CancellationToken cancellationToken)
    {
        try
        {
            var response =
                await _client.GetOrdersAsync(
                    instrumentId,
                    cancellationToken);

            return Ok(response.Orders);
        }
        catch (OrderQueryUnavailableException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable);
        }
    }
}