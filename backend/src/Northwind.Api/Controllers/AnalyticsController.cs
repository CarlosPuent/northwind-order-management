using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Infrastructure.Persistence;

namespace Northwind.Api.Controllers;

/// <summary>
/// Analytics endpoints for the dashboard. All endpoints support
/// optional year filtering so KPIs, charts, and tables stay consistent.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly NorthwindDbContext _db;

    public AnalyticsController(NorthwindDbContext db)
    {
        _db = db;
    }

    // =========================
    // DTOs
    // =========================
    public sealed record OrdersOverTimeDto(
        int Year,
        int Month,
        int OrderCount,
        decimal TotalRevenue
    );

    public sealed record ShipmentsByRegionDto(
        string Country,
        int OrderCount
    );

    public sealed record TopCustomerDto(
        string CustomerId,
        string CompanyName,
        int OrderCount,
        decimal TotalRevenue
    );

    // =========================
    // Orders Over Time
    // =========================
    /// <summary>
    /// Orders grouped by month. Used by the bar chart on the dashboard.
    /// </summary>
    [HttpGet("orders-over-time")]
    public async Task<ActionResult<List<OrdersOverTimeDto>>> OrdersOverTime(
        [FromQuery] int? year,
        CancellationToken ct)
    {
        var orders = await _db.Orders
            .AsNoTracking()
            .Where(o => !year.HasValue || o.OrderDate.Year == year.Value)
            .Select(o => new
            {
                o.Id,
                o.OrderDate,
                Lines = o.Lines.Select(l => new
                {
                    l.Quantity,
                    l.Discount,
                    Amount = l.UnitPrice.Amount
                })
            })
            .ToListAsync(ct);

        var result = orders
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new OrdersOverTimeDto(
                g.Key.Year,
                g.Key.Month,
                g.Count(),
                g.Sum(o =>
                    o.Lines.Sum(l =>
                        l.Amount *
                        l.Quantity *
                        (1m - (decimal)l.Discount)
                    )
                )
            ))
            .OrderBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToList();

        return Ok(result);
    }

    // =========================
    // Shipments By Region
    // =========================
    /// <summary>
    /// Shipments grouped by country. Supports optional year filter
    /// so the donut chart stays consistent with the selected year.
    /// </summary>
    [HttpGet("shipments-by-region")]
    public async Task<ActionResult<List<ShipmentsByRegionDto>>> ShipmentsByRegion(
        [FromQuery] int? year,
        CancellationToken ct)
    {
        var query = _db.Orders.AsNoTracking();

        if (year.HasValue)
            query = query.Where(o => o.OrderDate.Year == year.Value);

        var raw = await query
            .Select(o => new
            {
                Country = o.ShipAddress.Country
            })
            .ToListAsync(ct);

        var result = raw
            .Where(x => !string.IsNullOrWhiteSpace(x.Country))
            .GroupBy(x => x.Country!)
            .Select(g => new ShipmentsByRegionDto(
                g.Key,
                g.Count()
            ))
            .OrderByDescending(x => x.OrderCount)
            .Take(10)
            .ToList();

        return Ok(result);
    }

    // =========================
    // Top Customers
    // =========================
    /// <summary>
    /// Returns the top N customers by revenue for the given year.
    /// GET /api/analytics/top-customers?year=1997&amp;limit=5
    /// </summary>
    [HttpGet("top-customers")]
    public async Task<ActionResult<List<TopCustomerDto>>> TopCustomers(
        [FromQuery] int? year,
        [FromQuery] int limit = 5,
        CancellationToken ct = default)
    {
        var query = _db.Orders.AsNoTracking();

        if (year.HasValue)
            query = query.Where(o => o.OrderDate.Year == year.Value);

        var raw = await query
            .Select(o => new
            {
                o.CustomerId,
                Lines = o.Lines.Select(l => new
                {
                    l.Quantity,
                    l.Discount,
                    Amount = l.UnitPrice.Amount
                })
            })
            .ToListAsync(ct);

        // Join customer names from a separate query to avoid N+1
        var customerIds = raw.Select(o => o.CustomerId).Distinct().ToList();
        var customers = await _db.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.CompanyName })
            .ToDictionaryAsync(c => c.Id, c => c.CompanyName, ct);

        var result = raw
            .GroupBy(o => o.CustomerId)
            .Select(g => new TopCustomerDto(
                CustomerId: g.Key,
                CompanyName: customers.GetValueOrDefault(g.Key, g.Key),
                OrderCount: g.Count(),
                TotalRevenue: g.Sum(o =>
                    o.Lines.Sum(l =>
                        l.Amount * l.Quantity * (1m - (decimal)l.Discount)
                    )
                )
            ))
            .OrderByDescending(x => x.TotalRevenue)
            .Take(Math.Clamp(limit, 1, 20))
            .ToList();

        return Ok(result);
    }

    // =========================
    // Available Years
    // =========================
    /// <summary>
    /// Available years for the year filter dropdown.
    /// </summary>
    [HttpGet("available-years")]
    public async Task<ActionResult<List<int>>> AvailableYears(
        CancellationToken ct)
    {
        var years = await _db.Orders
            .AsNoTracking()
            .Select(o => o.OrderDate.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);

        return Ok(years);
    }
}