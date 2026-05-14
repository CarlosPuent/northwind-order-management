using Microsoft.AspNetCore.Mvc;
using Northwind.Application.Analytics;

namespace Northwind.Api.Controllers;

/// <summary>
/// Analytics endpoints for the dashboard. All endpoints support
/// optional year filtering so KPIs, charts, and tables stay consistent.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsRepository _analytics;

    public AnalyticsController(IAnalyticsRepository analytics)
    {
        _analytics = analytics;
    }

    /// <summary>
    /// Orders grouped by month. Used by the bar chart on the dashboard.
    /// </summary>
    [HttpGet("orders-over-time")]
    public async Task<ActionResult<List<OrdersOverTimeDto>>> OrdersOverTime(
        [FromQuery] int? year,
        CancellationToken ct)
        => Ok(await _analytics.GetOrdersOverTimeAsync(year, ct));

    /// <summary>
    /// Shipments grouped by country. Supports optional year filter
    /// so the donut chart stays consistent with the selected year.
    /// </summary>
    [HttpGet("shipments-by-region")]
    public async Task<ActionResult<List<ShipmentsByRegionDto>>> ShipmentsByRegion(
        [FromQuery] int? year,
        CancellationToken ct)
        => Ok(await _analytics.GetShipmentsByRegionAsync(year, ct));

    /// <summary>
    /// Returns the top N customers by revenue for the given year.
    /// GET /api/analytics/top-customers?year=1997&amp;limit=5
    /// </summary>
    [HttpGet("top-customers")]
    public async Task<ActionResult<List<TopCustomerDto>>> TopCustomers(
        [FromQuery] int? year,
        [FromQuery] int limit = 5,
        CancellationToken ct = default)
        => Ok(await _analytics.GetTopCustomersAsync(year, Math.Clamp(limit, 1, 20), ct));

    /// <summary>
    /// Available years for the year filter dropdown.
    /// </summary>
    [HttpGet("available-years")]
    public async Task<ActionResult<List<int>>> AvailableYears(CancellationToken ct)
        => Ok(await _analytics.GetAvailableYearsAsync(ct));

    /// <summary>
    /// Returns the most recently validated delivery locations from ShippingGeocodes.
    /// Used by the dashboard map widget.
    /// </summary>
    [HttpGet("delivery-locations")]
    public async Task<ActionResult<List<DeliveryLocationDto>>> DeliveryLocations(
        [FromQuery] int limit = 20,
        CancellationToken ct = default)
        => Ok(await _analytics.GetDeliveryLocationsAsync(Math.Clamp(limit, 1, 50), ct));
}
