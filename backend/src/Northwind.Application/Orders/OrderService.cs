using Northwind.Application.Abstractions.Persistence;
using Northwind.Application.Common;
using Northwind.Application.Orders.Commands;
using Northwind.Application.Orders.Dtos;
using Northwind.Domain.Common;
using Northwind.Domain.Entities;
using Northwind.Domain.ValueObjects;

namespace Northwind.Application.Orders;

/// <summary>
/// Application service that orchestrates Order use cases. Each method is a
/// complete use case: validate → delegate to domain → persist.
///
/// This service never calls EF Core directly — it depends on repository
/// abstractions and IUnitOfWork, which makes it fully unit-testable.
/// </summary>
public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IOrderRepository orders,
        IProductRepository products,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _products = products;
        _unitOfWork = unitOfWork;
    }

    // ------------------------------------------------------------------
    // Queries
    // ------------------------------------------------------------------

    public async Task<Result<OrderDto>> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return Error.NotFound("Order.NotFound", $"Order {id} was not found.");

        return MapToDto(order);
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? customerId = null,
        string? region = null,
        bool? isShipped = null, // <--- MODIFICACIÓN AQUÍ: Se agregó el parámetro
        CancellationToken cancellationToken = default)
    {
        // <--- MODIFICACIÓN AQUÍ: Se pasa isShipped al repositorio
        var result = await _orders.GetPagedAsync(page, pageSize, customerId, region, isShipped, cancellationToken);

        var dtos = result.Items.Select(MapToDto).ToList().AsReadOnly();
        return new PagedResult<OrderDto>(dtos, result.Page, result.PageSize, result.TotalCount);
    }

    // ------------------------------------------------------------------
    // Commands
    // ------------------------------------------------------------------

    public async Task<Result<OrderDto>> CreateAsync(
        CreateOrderCommand cmd,
        CancellationToken cancellationToken = default)
    {
        // 1. Build the Address value object.
        var addressResult = Address.Create(
            cmd.ShipStreet, cmd.ShipCity, cmd.ShipRegion,
            cmd.ShipPostalCode, cmd.ShipCountry);

        if (addressResult.IsFailure)
            return addressResult.Error;

        // 2. Build the Freight Money value.
        var freightResult = Money.Create(cmd.Freight, "USD");
        if (freightResult.IsFailure)
            return freightResult.Error;

        // 3. Create the Order via the domain factory (validates business rules).
        var orderResult = Order.Create(
            cmd.CustomerId, cmd.EmployeeId, cmd.OrderDate,
            cmd.ShipName, addressResult.Value, freightResult.Value);

        if (orderResult.IsFailure)
            return orderResult.Error;

        var order = orderResult.Value;

        // 4. Add lines. Each AddLine call validates and may merge duplicates.
        if (cmd.Lines == null || cmd.Lines.Count == 0)
            return Error.Validation("Order.NoLines", "Order must have at least one line.");

        foreach (var line in cmd.Lines)
        {
            var product = await _products.GetByIdAsync(line.ProductId, cancellationToken);
            if (product is null)
                return Error.NotFound(
                    "Order.ProductNotFound",
                    $"Product {line.ProductId} was not found.");

            var lineResult = order.AddLine(
                line.ProductId,
                product.UnitPrice,
                line.Quantity,
                line.Discount);

            if (lineResult.IsFailure)
                return lineResult.Error;
        }

        // 5. Persist.
        _orders.Add(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(order);
    }

    public async Task<Result<OrderDto>> UpdateAsync(
        UpdateOrderCommand cmd,
        CancellationToken cancellationToken = default)
    {
        // 1. Load existing order.
        var order = await _orders.GetByIdAsync(cmd.OrderId, cancellationToken);
        if (order is null)
            return Error.NotFound("Order.NotFound", $"Order {cmd.OrderId} was not found.");

        if (!order.IsEditable)
            return Error.Conflict("Order.NotEditable", "Cannot modify an order that has been shipped.");

        // 2. Update address.
        var addressResult = Address.Create(
            cmd.ShipStreet, cmd.ShipCity, cmd.ShipRegion,
            cmd.ShipPostalCode, cmd.ShipCountry);

        if (addressResult.IsFailure)
            return addressResult.Error;

        var updateAddress = order.UpdateShipAddress(addressResult.Value);
        if (updateAddress.IsFailure)
            return updateAddress.Error;

        var updateName = order.UpdateShipName(cmd.ShipName);
        if (updateName.IsFailure)
            return updateName.Error;

        // 3. Update shipper if provided.
        if (cmd.ShipperId.HasValue)
        {
            var assignResult = order.AssignShipper(cmd.ShipperId.Value);
            if (assignResult.IsFailure)
                return assignResult.Error;
        }

        var existingProductIds = order.Lines.Select(l => l.ProductId).ToList();
        foreach (var pid in existingProductIds)
            order.RemoveLine(pid);

        if (cmd.Lines == null || cmd.Lines.Count == 0)
            return Error.Validation("Order.NoLines", "Order must have at least one line.");

        foreach (var line in cmd.Lines)
        {
            var product = await _products.GetByIdAsync(line.ProductId, cancellationToken);
            if (product is null)
                return Error.NotFound(
                    "Order.ProductNotFound",
                    $"Product {line.ProductId} was not found.");

            var lineResult = order.AddLine(
                line.ProductId,
                product.UnitPrice,
                line.Quantity,
                line.Discount);

            if (lineResult.IsFailure)
                return lineResult.Error;
        }

        // 5. Persist. EF Core tracks the order — SaveChanges emits the UPDATEs.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToDto(order);
    }

    public async Task<Result> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken);
        if (order is null)
            return Error.NotFound("Order.NotFound", $"Order {id} was not found.");

        if (order.IsShipped)
            return Error.Conflict("Order.AlreadyShipped", "Cannot delete an order that has been shipped.");

        _orders.Remove(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    // ------------------------------------------------------------------
    // Private mapping
    // ------------------------------------------------------------------

    private static OrderDto MapToDto(Order order) => new()
    {
        Id = order.Id,
        CustomerId = order.CustomerId,
        EmployeeId = order.EmployeeId,
        ShipperId = order.ShipperId,
        OrderDate = order.OrderDate,
        RequiredDate = order.RequiredDate,
        ShippedDate = order.ShippedDate,
        IsShipped = order.IsShipped,
        Freight = order.Freight.Amount,
        ShipName = order.ShipName,
        ShipStreet = order.ShipAddress.Street,
        ShipCity = order.ShipAddress.City,
        ShipRegion = order.ShipAddress.Region,
        ShipPostalCode = order.ShipAddress.PostalCode,
        ShipCountry = order.ShipAddress.Country,
        SubTotal = order.SubTotal.Amount,
        Total = order.Total.Amount,
        Lines = order.Lines.Select(l => new OrderLineDto
        {
            ProductId = l.ProductId,
            ProductName = string.Empty, // Populated when we join with Products
            UnitPrice = l.UnitPrice.Amount,
            Quantity = l.Quantity,
            Discount = l.Discount,
            LineTotal = l.LineTotal.Amount
        }).ToList()
    };
}