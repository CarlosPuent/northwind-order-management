using Microsoft.AspNetCore.Mvc;
using Northwind.Application.Abstractions;
using Northwind.Application.Abstractions.Persistence;

namespace Northwind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class InvoicesController : ControllerBase
{
    private readonly IOrderRepository _orders;
    private readonly ICustomerRepository _customers;
    private readonly IEmployeeRepository _employees;
    private readonly IShipperRepository _shippers;
    private readonly IShippingGeocodeRepository _geocodes;
    private readonly IInvoiceGenerator _invoiceGenerator;

    public InvoicesController(
        IOrderRepository orders,
        ICustomerRepository customers,
        IEmployeeRepository employees,
        IShipperRepository shippers,
        IShippingGeocodeRepository geocodes,
        IInvoiceGenerator invoiceGenerator)
    {
        _orders = orders;
        _customers = customers;
        _employees = employees;
        _shippers = shippers;
        _geocodes = geocodes;
        _invoiceGenerator = invoiceGenerator;
    }

    /// <summary>
    /// Generates and returns a styled PDF invoice for the given order.
    /// GET /api/invoices/10248
    /// </summary>
    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> Generate(int orderId, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return NotFound(new { error = $"Order {orderId} not found." });

        var customer = await _customers.GetByIdAsync(order.CustomerId, cancellationToken);
        var employee = await _employees.GetByIdAsync(order.EmployeeId, cancellationToken);

        string? shipperName = null;
        if (order.ShipperId.HasValue)
        {
            var shipper = await _shippers.GetByIdAsync(order.ShipperId.Value, cancellationToken);
            shipperName = shipper?.CompanyName;
        }

        var geocode = await _geocodes.GetByOrderIdAsync(orderId, cancellationToken);

        var result = await _invoiceGenerator.GenerateAsync(
            order,
            customer?.CompanyName ?? "Unknown Customer",
            employee != null ? $"{employee.FirstName} {employee.LastName}" : "Unknown Employee",
            shipperName,
            geocode,
            cancellationToken);

        if (result.IsFailure)
            return StatusCode(500, new { error = result.Error.Message });

        return File(result.Value, "application/pdf", $"Invoice-{orderId}.pdf");
    }
}