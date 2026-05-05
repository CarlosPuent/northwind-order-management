using Microsoft.AspNetCore.Mvc;
using Northwind.Application.Orders;
using Northwind.Application.Orders.Commands;
using Northwind.Domain.Common;

namespace Northwind.Api.Controllers;

/// <summary>
/// Manages customer orders through their full lifecycle:
/// create → edit → ship → invoice.
/// Status is derived from ShippedDate — no separate status column exists in Northwind.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public sealed class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;

    public OrdersController(OrderService orderService)
    {
        _orderService = orderService;
    }

    // ------------------------------------------------------------------
    // GET /api/orders
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns a paginated list of orders with optional filters.
    /// All filters are combinable — e.g. customerId + region + year.
    /// </summary>
    /// <param name="page">Page number, 1-based. Default: 1.</param>
    /// <param name="pageSize">Items per page. Default: 20.</param>
    /// <param name="customerId">Filter by exact customer ID (e.g. ALFKI).</param>
    /// <param name="region">
    /// Partial match against ShipRegion, ShipCountry, and ShipCity.
    /// E.g. "Germany", "Berlin", or "Western Europe" all work.
    /// </param>
    /// <param name="isShipped">
    /// true = shipped orders only, false = pending only, null = all orders.
    /// Derived from ShippedDate — null means pending.
    /// </param>
    /// <param name="fromDate">Filter orders placed on or after this date (ISO 8601).</param>
    /// <param name="toDate">Filter orders placed on or before this date (ISO 8601).</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? customerId = null,
        [FromQuery] string? region = null,
        [FromQuery] bool? isShipped = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _orderService.GetPagedAsync(
            page, pageSize, customerId, region, isShipped, fromDate, toDate, cancellationToken);
        return Ok(result);
    }

    // ------------------------------------------------------------------
    // GET /api/orders/{id}
    // ------------------------------------------------------------------

    /// <summary>
    /// Returns a single order with all line items, shipping details, and totals.
    /// Product names are resolved from the Products table.
    /// </summary>
    /// <param name="id">The order ID.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.GetByIdAsync(id, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : ToProblemDetails(result.Error);
    }

    // ------------------------------------------------------------------
    // POST /api/orders
    // ------------------------------------------------------------------

    /// <summary>
    /// Creates a new order with at least one line item.
    /// If the shipping address was validated via the geocoding endpoint before
    /// submitting, include the geocode result in the request body —
    /// it will be saved to the ShippingGeocodes table and appear on the invoice map.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateOrderCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _orderService.CreateAsync(command, cancellationToken);
        return result.IsSuccess
            ? CreatedAtAction(nameof(GetById), new { id = result.Value.Id }, result.Value)
            : ToProblemDetails(result.Error);
    }

    // ------------------------------------------------------------------
    // PUT /api/orders/{id}
    // ------------------------------------------------------------------

    /// <summary>
    /// Updates an existing pending order — address, lines, freight, and shipper.
    /// Shipped orders cannot be modified (returns 409 Conflict).
    /// If a new geocode is provided, the existing ShippingGeocode record is replaced.
    /// </summary>
    /// <param name="id">The order ID. Must match the OrderId in the request body.</param>
    /// <param name="command">Order update payload.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateOrderCommand command,
        CancellationToken cancellationToken)
    {
        if (id != command.OrderId)
            return BadRequest(new ProblemDetails
            {
                Title = "ID mismatch",
                Detail = "The ID in the URL does not match the OrderId in the request body.",
                Status = StatusCodes.Status400BadRequest
            });

        var result = await _orderService.UpdateAsync(command, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : ToProblemDetails(result.Error);
    }

    // ------------------------------------------------------------------
    // POST /api/orders/{id}/ship
    // ------------------------------------------------------------------

    /// <summary>
    /// Marks an order as shipped by setting ShippedDate.
    /// Requires a shipper to be assigned — the domain enforces this invariant.
    /// This action is irreversible: ShippedDate is the single source of truth
    /// for IsShipped, and once set it cannot be cleared via the API.
    /// </summary>
    /// <param name="id">The order ID to ship.</param>
    /// <param name="request">Ship request payload.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpPost("{id:int}/ship")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Ship(
        int id,
        [FromBody] ShipOrderRequest request,
        CancellationToken cancellationToken)
    {
        var cmd = new ShipOrderCommand(id, request.ShipperId, request.ShippedDate);
        var result = await _orderService.ShipAsync(cmd, cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : ToProblemDetails(result.Error);
    }

    // ------------------------------------------------------------------
    // DELETE /api/orders/{id}
    // ------------------------------------------------------------------

    /// <summary>
    /// Permanently deletes a pending order including all line items.
    /// Shipped orders cannot be deleted (returns 409 Conflict).
    /// </summary>
    /// <param name="id">The order ID to delete.</param>
    /// <param name="cancellationToken">Request cancellation token.</param>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var result = await _orderService.DeleteAsync(id, cancellationToken);
        return result.IsSuccess
            ? NoContent()
            : ToProblemDetails(result.Error);
    }

    // ------------------------------------------------------------------
    // Request DTOs
    // ------------------------------------------------------------------

    /// <summary>Request body for the Ship endpoint.</summary>
    /// <param name="ShipperId">The shipper that will carry this order. Required.</param>
    /// <param name="ShippedDate">
    /// The actual ship date. If null, the server uses the current UTC time.
    /// </param>
    public sealed record ShipOrderRequest(
        int ShipperId,
        DateTime? ShippedDate = null);

    // ------------------------------------------------------------------
    // Error mapping — RFC 7807 ProblemDetails
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