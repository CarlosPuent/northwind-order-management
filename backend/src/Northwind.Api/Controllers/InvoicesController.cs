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
    private readonly IProductRepository _products;
    private readonly IInvoiceGenerator _invoiceGenerator;

    public InvoicesController(
        IOrderRepository orders,
        ICustomerRepository customers,
        IEmployeeRepository employees,
        IShipperRepository shippers,
        IShippingGeocodeRepository geocodes,
        IProductRepository products,
        IInvoiceGenerator invoiceGenerator)
    {
        _orders = orders;
        _customers = customers;
        _employees = employees;
        _shippers = shippers;
        _geocodes = geocodes;
        _products = products;
        _invoiceGenerator = invoiceGenerator;
    }

    /// <summary>GET /api/invoices/10248</summary>
    [HttpGet("{orderId:int}")]
    public async Task<IActionResult> Generate(int orderId, CancellationToken cancellationToken)
    {
        var order = await _orders.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
            return NotFound(new { error = $"Order {orderId} not found." });

        // Sequential await — EF Core DbContext cannot run parallel queries.
        var customer = await _customers.GetByIdAsync(order.CustomerId, cancellationToken);
        var employee = await _employees.GetByIdAsync(order.EmployeeId, cancellationToken);
        var geocode = await _geocodes.GetByOrderIdAsync(orderId, cancellationToken);

        string? shipperName = null;
        if (order.ShipperId.HasValue)
        {
            var shipper = await _shippers.GetByIdAsync(order.ShipperId.Value, cancellationToken);
            shipperName = shipper?.CompanyName;
        }

        // Load product names for all line items.
        var productNames = new Dictionary<int, string>();
        foreach (var pid in order.Lines.Select(l => l.ProductId).Distinct())
        {
            var product = await _products.GetByIdAsync(pid, cancellationToken);
            if (product is not null)
                productNames[pid] = product.ProductName;
        }

        var result = await _invoiceGenerator.GenerateAsync(
            order,
            customer?.CompanyName ?? "Unknown Customer",
            employee != null ? $"{employee.FirstName} {employee.LastName}" : "Unknown Employee",
            shipperName,
            geocode,
            productNames,
            cancellationToken);

        if (result.IsFailure)
            return StatusCode(500, new { error = result.Error.Message });

        return File(result.Value, "application/pdf", $"Invoice-{orderId:D6}.pdf");
    }
}