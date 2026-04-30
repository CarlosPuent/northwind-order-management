using Northwind.Application.Common;
using Northwind.Domain.Entities;

namespace Northwind.Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for the Order aggregate.
/// Implementations live in the Infrastructure project and use EF Core.
/// </summary>
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<Order>> GetPagedAsync(
        int page,
        int pageSize,
        string? customerId = null,
        string? region = null,
        bool? isShipped = null, // <--- NUEVO PARÁMETRO
        CancellationToken cancellationToken = default);

    void Add(Order order);
    void Remove(Order order);
}