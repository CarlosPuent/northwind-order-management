using Microsoft.AspNetCore.Mvc;
using Northwind.Application.Abstractions.Persistence;

namespace Northwind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class ShippersController : ControllerBase
{
    private readonly IShipperRepository _shippers;

    public ShippersController(IShipperRepository shippers) => _shippers = shippers;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var shippers = await _shippers.GetAllAsync(cancellationToken);
        var result = shippers.Select(s => new
        {
            s.Id,
            s.CompanyName,
            s.Phone
        });
        return Ok(result);
    }
}