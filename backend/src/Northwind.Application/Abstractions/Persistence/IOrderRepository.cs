using Northwind.Application.Common;
using Northwind.Domain.Entities;

namespace Northwind.Application.Abstractions.Persistence;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<Order>> GetPagedAsync(
    int page,
    int pageSize,
    string? customerId = null,
    string? region = null,
    bool? isShipped = null,
    DateTime? fromDate = null,
    DateTime? toDate = null,
    CancellationToken cancellationToken = default);

    void Add(Order order);
    void Remove(Order order);
}