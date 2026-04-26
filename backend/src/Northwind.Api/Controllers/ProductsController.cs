using Microsoft.AspNetCore.Mvc;
using Northwind.Application.Abstractions.Persistence;

namespace Northwind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductRepository _products;

    public ProductsController(IProductRepository products) => _products = products;

    /// <summary>
    /// Searches active (non-discontinued) products by name.
    /// GET /api/products/search?q=chai
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<object>());

        var products = await _products.SearchByNameAsync(q.Trim(), 10, cancellationToken);
        var result = products.Select(p => new
        {
            p.Id,
            p.ProductName,
            UnitPrice = p.UnitPrice.Amount,
            p.UnitsInStock
        });
        return Ok(result);
    }
}