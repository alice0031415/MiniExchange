using Microsoft.AspNetCore.Mvc;
using MiniExchange.Contracts.Grpc;
using MiniExchange.ReadApi.Services;

namespace MiniExchange.ReadApi.Controllers;

[ApiController]
[Route("trades")]
public sealed class TradesController : ControllerBase
{
    private readonly OrderQueryClient _client;

    public TradesController(
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
                await _client.GetTradesAsync(
                    instrumentId,
                    cancellationToken);

            return Ok(response.Trades);
        }
        catch (OrderQueryUnavailableException)
        {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable);
        }
    }
}