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
        => await _db.Orders
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task<PagedResult<Order>> GetPagedAsync(
        int page,
        int pageSize,
        string? customerId = null,
        string? region = null,
        CancellationToken cancellationToken = default)
    {
        // Start with the full set of orders.
        var query = _db.Orders.AsNoTracking().AsQueryable();

        // Apply filters only when provided — composable LINQ builds a single SQL query.
        if (!string.IsNullOrWhiteSpace(customerId))
            query = query.Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(region))
            query = query.Where(o => o.ShipAddress.Region == region);

        // Count before pagination (total across all pages).
        var totalCount = await query.CountAsync(cancellationToken);

        // Fetch the requested page, newest first.
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