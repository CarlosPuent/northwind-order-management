using Microsoft.AspNetCore.Mvc;
using Northwind.Application.Abstractions.Persistence;

namespace Northwind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class EmployeesController : ControllerBase
{
    private readonly IEmployeeRepository _employees;

    public EmployeesController(IEmployeeRepository employees) => _employees = employees;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var employees = await _employees.GetAllAsync(cancellationToken);
        var result = employees.Select(e => new
        {
            e.Id,
            e.FirstName,
            e.LastName,
            FullName = $"{e.FirstName} {e.LastName}",
            e.Title,
            e.City,
            e.Country
        });
        return Ok(result);
    }
}