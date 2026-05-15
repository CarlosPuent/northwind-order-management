using Microsoft.EntityFrameworkCore;
using Northwind.Application.Analytics;

namespace Northwind.Infrastructure.Persistence.Repositories;

public sealed class AnalyticsRepository : IAnalyticsRepository
{
    private readonly NorthwindDbContext _db;

    public AnalyticsRepository(NorthwindDbContext db)
    {
        _db = db;
    }

    public async Task<List<OrdersOverTimeDto>> GetOrdersOverTimeAsync(
        int? year, CancellationToken ct = default)
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

        return orders
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
    }

    public async Task<List<ShipmentsByRegionDto>> GetShipmentsByRegionAsync(
        int? year, CancellationToken ct = default)
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

        return raw
            .Where(x => !string.IsNullOrWhiteSpace(x.Country))
            .GroupBy(x => x.Country!)
            .Select(g => new ShipmentsByRegionDto(
                g.Key,
                g.Count()
            ))
            .OrderByDescending(x => x.OrderCount)
            .Take(10)
            .ToList();
    }

    public async Task<List<TopCustomerDto>> GetTopCustomersAsync(
        int? year, int limit, CancellationToken ct = default)
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

        var customerIds = raw.Select(o => o.CustomerId).Distinct().ToList();
        var customers = await _db.Customers
            .AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .Select(c => new { c.Id, c.CompanyName })
            .ToDictionaryAsync(c => c.Id, c => c.CompanyName, ct);

        return raw
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
            .Take(limit)
            .ToList();
    }

    public async Task<List<int>> GetAvailableYearsAsync(CancellationToken ct = default)
    {
        return await _db.Orders
            .AsNoTracking()
            .Select(o => o.OrderDate.Year)
            .Distinct()
            .OrderByDescending(y => y)
            .ToListAsync(ct);
    }

    public async Task<List<DeliveryLocationDto>> GetDeliveryLocationsAsync(
        int limit, CancellationToken ct = default)
    {
        return await _db.ShippingGeocodes
            .AsNoTracking()
            .OrderByDescending(g => g.ValidatedAt)
            .Take(limit)
            .Select(g => new DeliveryLocationDto(
                g.OrderId,
                g.Coordinates.Latitude,
                g.Coordinates.Longitude,
                g.StandardizedAddress.City,
                g.StandardizedAddress.Country,
                g.PlaceType,
                g.ValidatedAt
            ))
            .ToListAsync(ct);
    }
}
