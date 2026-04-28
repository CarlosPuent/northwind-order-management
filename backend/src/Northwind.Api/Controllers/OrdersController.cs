using Microsoft.AspNetCore.Mvc;
using Northwind.Application.Orders;
using Northwind.Application.Orders.Commands;
using Northwind.Domain.Common;

namespace Northwind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>GET /api/orders?page=1&amp;pageSize=20&amp;customerId=ALFKI&amp;region=Western Europe</summary>
    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? customerId = null,
        [FromQuery] string? region = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetPagedAsync(page, pageSize, customerId, region, cancellationToken);
        return Ok(result);
    }

    /// <summary>GET /api/orders/10248</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : ToProblemDetails(result.Error);
    }

    /// <summary>POST /api/orders</summary>
    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.CreateAsync(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : ToProblemDetails(result.Error);
    }

    /// <summary>PUT /api/orders/10248</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateOrderCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.OrderId)
            return BadRequest(new ProblemDetails
            {
                Title = "ID mismatch",
                Detail = "The ID in the URL does not match the ID in the request body.",
                Status = 400
            });

        var result = await _orderService.UpdateAsync(command, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : ToProblemDetails(result.Error);
    }

    /// <summary>DELETE /api/orders/10248</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : ToProblemDetails(result.Error);
    }

    // ------------------------------------------------------------------
    // Maps domain errors to RFC 7807 ProblemDetails responses.
    // Each ErrorType maps to a specific HTTP status code.
    // ------------------------------------------------------------------
    private IActionResult ToProblemDetails(Error error)
    {
        var statusCode = error.Type switch
        {
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(
            title: error.Code,
            detail: error.Message,
            statusCode: statusCode);
    }
}