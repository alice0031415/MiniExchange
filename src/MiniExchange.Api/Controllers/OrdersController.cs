using Microsoft.AspNetCore.Mvc;
using MiniExchange.Api.Contracts;
using MiniExchange.Application;
using MiniExchange.Application.Orders;
using MiniExchange.Domain;
using MiniExchange.Application.OrderBooks;

namespace MiniExchange.Api.Controllers;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly ITradingService _tradingService;
    private readonly IOrderRepository _orders;
    private readonly IOrderBookCache _orderBookCache;

    public OrdersController(
        ITradingService tradingService,
        IOrderRepository orders,
        IOrderBookCache orderBookCache)
    {
        _tradingService = tradingService;
        _orders = orders;
        _orderBookCache = orderBookCache;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> Place(
        PlaceOrderRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var order = await _tradingService.PlaceOrder(
                request.InstrumentId,
                request.Side,
                request.Price,
                request.Quantity,
                cancellationToken);

            return Created(
                $"/orders/{order.Id}",
                order);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Order>>> Get(
        [FromQuery] Guid? instrumentId,
        CancellationToken cancellationToken)
    {
        var orders = await _orders.GetOrdersAsync(
            instrumentId,
            cancellationToken);

        return Ok(orders);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Cancel(
        Guid id,
        CancellationToken cancellationToken)
    {
        try
        {
            await _tradingService.CancelOrder(
                id,
                cancellationToken);

            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message);
        }
    }
}