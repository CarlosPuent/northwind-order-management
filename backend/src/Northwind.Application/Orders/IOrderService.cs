using Northwind.Application.Common;
using Northwind.Application.Orders.Commands;
using Northwind.Application.Orders.Dtos;
using Northwind.Domain.Common;

namespace Northwind.Application.Orders;

public interface IOrderService
{
    Task<Result<OrderDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<OrderDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? customerId = null,
        string? region = null,
        bool? isShipped = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default);

    Task<Result<OrderDto>> CreateAsync(CreateOrderCommand cmd, CancellationToken cancellationToken = default);

    Task<Result<OrderDto>> UpdateAsync(UpdateOrderCommand cmd, CancellationToken cancellationToken = default);

    Task<Result<OrderDto>> ShipAsync(ShipOrderCommand cmd, CancellationToken cancellationToken = default);

    Task<Result> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
