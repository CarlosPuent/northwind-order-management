using Microsoft.EntityFrameworkCore;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Application.Common;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Repositories;

internal sealed class OrderRepository : IOrderRepository
{
    private readonly NorthwindDbContext _db;

    public OrderRepository(NorthwindDbContext db) => _db = db;

    public async Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _db.Orders.FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<PagedResult<Order>> GetPagedAsync(
        int page,
        int pageSize,
        string? customerId = null,
        string? region = null,
        bool? isShipped = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(customerId))
            query = query.Where(o => o.CustomerId == customerId);

        // Searches Region, Country, AND City so "Germany", "Berlin",
        // and "Western Europe" all return relevant results.
        if (!string.IsNullOrWhiteSpace(region))
        {
            var needle = region.Trim().ToLower();
            query = query.Where(o =>
                (o.ShipAddress.Region != null && o.ShipAddress.Region.ToLower().Contains(needle)) ||
                (o.ShipAddress.Country != null && o.ShipAddress.Country.ToLower().Contains(needle)) ||
                (o.ShipAddress.City != null && o.ShipAddress.City.ToLower().Contains(needle)));
        }

        // Filter by shipped status derived from ShippedDate.
        if (isShipped.HasValue)
        {
            if (isShipped.Value)
                query = query.Where(o => o.ShippedDate != null);
            else
                query = query.Where(o => o.ShippedDate == null);
        }

        if (fromDate.HasValue)
            query = query.Where(o => o.OrderDate >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(o => o.OrderDate <= toDate.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Order>(items, page, pageSize, totalCount);
    }

    public void Add(Order order) => _db.Orders.Add(order);

    public void Remove(Order order) => _db.Orders.Remove(order);
}