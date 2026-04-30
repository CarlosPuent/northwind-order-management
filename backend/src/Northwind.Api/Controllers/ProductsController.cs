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

    /// <summary>
    /// Gets default active products for initial load.
    /// GET /api/products
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetInitialProducts(CancellationToken cancellationToken)
    {
        // Reutilizamos tu método de búsqueda pasándole un string vacío para engañarlo
        // y le pedimos un límite razonable (ej. 50) para no saturar la página.
        // Nota: Asegúrate de que tu IProductRepository soporte strings vacíos en la consulta DB.
        var products = await _products.SearchByNameAsync("", 50, cancellationToken);

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