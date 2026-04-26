using Northwind.Application.Common;
using Northwind.Domain.Entities;

namespace Northwind.Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for the Order aggregate.
/// Implementations live in the Infrastructure project and use EF Core.
/// </summary>
public interface IOrderRepository
{
    /// <summary>
    /// Loads an order along with its lines. Returns null if not found.
    /// </summary>
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a paginated list of orders, newest first.
    /// </summary>
    /// <param name="page">1-based page number.</param>
    /// <param name="pageSize">Items per page.</param>
    /// <param name="customerId">Optional filter by customer.</param>
    /// <param name="region">Optional filter by ship region.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<PagedResult<Order>> GetPagedAsync(
        int page,
        int pageSize,
        string? customerId = null,
        string? region = null,
        CancellationToken cancellationToken = default);

    /// <summary>Stages a new order for insertion. Persisted via IUnitOfWork.</summary>
    void Add(Order order);

    /// <summary>Stages an order for deletion. Persisted via IUnitOfWork.</summary>
    void Remove(Order order);
}