using Microsoft.AspNetCore.Mvc;
using Northwind.Application.Abstractions.Persistence;

namespace Northwind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customers;

    public CustomersController(ICustomerRepository customers)
    {
        _customers = customers;
    }

    /// <summary>
    /// Returns all customers ordered by company name.
    /// Used to populate the customer dropdown in the order form.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var customers = await _customers.GetAllAsync(cancellationToken);

        var result = customers.Select(c => new
        {
            c.Id,
            c.CompanyName,
            c.ContactName,
            c.City,
            c.Region,
            c.Country,
            c.Phone
        });

        return Ok(result);
    }

    /// <summary>
    /// Searches customers by company name fragment. Returns up to 10 matches.
    /// Used by the order form's autocomplete.
    /// </summary>
    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string q,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(q))
            return Ok(Array.Empty<object>());

        var customers = await _customers.SearchByNameAsync(q.Trim(), 10, cancellationToken);

        var result = customers.Select(c => new
        {
            c.Id,
            c.CompanyName,
            c.ContactName,
            c.City,
            c.Country
        });

        return Ok(result);
    }
}