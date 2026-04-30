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
        bool? isShipped = null, // <--- NUEVO PARÁMETRO
        CancellationToken cancellationToken = default)
    {
        var query = _db.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(customerId))
            query = query.Where(o => o.CustomerId == customerId);

        if (!string.IsNullOrWhiteSpace(region))
        {
            var needle = region.Trim().ToLower();
            query = query.Where(o =>
                (o.ShipAddress.Region != null && o.ShipAddress.Region.ToLower().Contains(needle)) ||
                (o.ShipAddress.Country != null && o.ShipAddress.Country.ToLower().Contains(needle)) ||
                (o.ShipAddress.City != null && o.ShipAddress.City.ToLower().Contains(needle))
            );
        }

        // <--- NUEVA LÓGICA DE FILTRO POR STATUS --->
        if (isShipped.HasValue)
        {
            if (isShipped.Value)
            {
                // IsShipped = true -> ShippedDate tiene valor
                query = query.Where(o => o.ShippedDate != null);
            }
            else
            {
                // IsShipped = false -> ShippedDate es null (Pending)
                query = query.Where(o => o.ShippedDate == null);
            }
        }

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