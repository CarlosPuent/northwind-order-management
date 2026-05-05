using Northwind.Application.Abstractions;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Application.Common;
using Northwind.Application.Orders.Commands;
using Northwind.Application.Orders.Dtos;
using Northwind.Domain.Common;
using Northwind.Domain.Entities;
using Northwind.Domain.ValueObjects;

namespace Northwind.Application.Orders;

public sealed class OrderService
{
    private readonly IOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly IShippingGeocodeRepository _geocodes;
    private readonly IUnitOfWork _unitOfWork;

    public OrderService(
        IOrderRepository orders,
        IProductRepository products,
        IShippingGeocodeRepository geocodes,
        IUnitOfWork unitOfWork)
    {
        _orders = orders;
        _products = products;
        _geocodes = geocodes;
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

        // FIX: Load product names for the lines so the detail page
        // shows actual product names instead of empty strings.
        var productNames = new Dictionary<int, string>();
        foreach (var pid in order.Lines.Select(l => l.ProductId).Distinct())
        {
            var product = await _products.GetByIdAsync(pid, cancellationToken);
            if (product is not null)
                productNames[pid] = product.ProductName;
        }

        return MapToDto(order, productNames);
    }

    public async Task<PagedResult<OrderDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? customerId = null,
        string? region = null,
        bool? isShipped = null,
        CancellationToken cancellationToken = default)
    {
        var result = await _orders.GetPagedAsync(page, pageSize, customerId, region, isShipped, cancellationToken);
        var dtos = result.Items.Select(o => MapToDto(o)).ToList().AsReadOnly();
        return new PagedResult<OrderDto>(dtos, result.Page, result.PageSize, result.TotalCount);
    }

    // ------------------------------------------------------------------
    // Commands
    // ------------------------------------------------------------------

    public async Task<Result<OrderDto>> CreateAsync(
        CreateOrderCommand cmd,
        CancellationToken cancellationToken = default)
    {
        var addressResult = Address.Create(
            cmd.ShipStreet, cmd.ShipCity, cmd.ShipRegion,
            cmd.ShipPostalCode, cmd.ShipCountry);
        if (addressResult.IsFailure) return addressResult.Error;

        var freightResult = Money.Create(cmd.Freight, "USD");
        if (freightResult.IsFailure) return freightResult.Error;

        var orderResult = Order.Create(
            cmd.CustomerId, cmd.EmployeeId, cmd.OrderDate,
            cmd.ShipName, addressResult.Value, freightResult.Value);
        if (orderResult.IsFailure) return orderResult.Error;

        var order = orderResult.Value;

        if (cmd.Lines == null || cmd.Lines.Count == 0)
            return Error.Validation("Order.NoLines", "Order must have at least one line.");

        foreach (var line in cmd.Lines)
        {
            var product = await _products.GetByIdAsync(line.ProductId, cancellationToken);
            if (product is null)
                return Error.NotFound("Order.ProductNotFound", $"Product {line.ProductId} was not found.");

            var lineResult = order.AddLine(line.ProductId, product.UnitPrice, line.Quantity, line.Discount);
            if (lineResult.IsFailure) return lineResult.Error;
        }

        _orders.Add(order);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // FIX: Save geocode if the user validated the address before submitting.
        // order.Id is now set by EF Core after SaveChanges.
        if (cmd.Geocode is not null && order.Id > 0)
        {
            await SaveGeocodeAsync(order.Id, addressResult.Value, cmd.Geocode, cancellationToken);
        }

        return MapToDto(order);
    }

    public async Task<Result<OrderDto>> UpdateAsync(
        UpdateOrderCommand cmd,
        CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(cmd.OrderId, cancellationToken);
        if (order is null)
            return Error.NotFound("Order.NotFound", $"Order {cmd.OrderId} was not found.");

        if (!order.IsEditable)
            return Error.Conflict("Order.NotEditable", "Cannot modify an order that has been shipped.");

        var addressResult = Address.Create(
            cmd.ShipStreet, cmd.ShipCity, cmd.ShipRegion,
            cmd.ShipPostalCode, cmd.ShipCountry);
        if (addressResult.IsFailure) return addressResult.Error;

        var updateAddress = order.UpdateShipAddress(addressResult.Value);
        if (updateAddress.IsFailure) return updateAddress.Error;

        var updateName = order.UpdateShipName(cmd.ShipName);
        if (updateName.IsFailure) return updateName.Error;

        if (cmd.ShipperId.HasValue)
        {
            var assignResult = order.AssignShipper(cmd.ShipperId.Value);
            if (assignResult.IsFailure) return assignResult.Error;
        }

        var existingProductIds = order.Lines.Select(l => l.ProductId).ToList();
        foreach (var pid in existingProductIds) order.RemoveLine(pid);

        if (cmd.Lines == null || cmd.Lines.Count == 0)
            return Error.Validation("Order.NoLines", "Order must have at least one line.");

        foreach (var line in cmd.Lines)
        {
            var product = await _products.GetByIdAsync(line.ProductId, cancellationToken);
            if (product is null)
                return Error.NotFound("Order.ProductNotFound", $"Product {line.ProductId} was not found.");

            var lineResult = order.AddLine(line.ProductId, product.UnitPrice, line.Quantity, line.Discount);
            if (lineResult.IsFailure) return lineResult.Error;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // FIX: Update geocode if address was re-validated.
        if (cmd.Geocode is not null)
        {
            await SaveGeocodeAsync(order.Id, addressResult.Value, cmd.Geocode, cancellationToken);
        }

        return MapToDto(order);
    }

    /// <summary>
    /// Marks an order as shipped. This sets ShippedDate which is the source
    /// of truth for IsShipped — Northwind has no Status column.
    /// </summary>
    public async Task<Result<OrderDto>> ShipAsync(
       ShipOrderCommand cmd,
       CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(cmd.OrderId, cancellationToken);
        if (order is null)
            return Error.NotFound("Order.NotFound", $"Order {cmd.OrderId} was not found.");

        var assignResult = order.AssignShipper(cmd.ShipperId);
        if (assignResult.IsFailure) return assignResult.Error;

        var markResult = order.MarkAsShipped(cmd.ShippedDate ?? DateTime.UtcNow);
        if (markResult.IsFailure) return markResult.Error;

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var productNames = new Dictionary<int, string>();
        foreach (var pid in order.Lines.Select(l => l.ProductId).Distinct())
        {
            var product = await _products.GetByIdAsync(pid, cancellationToken);
            if (product is not null)
                productNames[pid] = product.ProductName;
        }

        return MapToDto(order, productNames);
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
    // Private helpers
    // ------------------------------------------------------------------

    private async Task SaveGeocodeAsync(
        int orderId,
        Address address,
        GeocodeCommandData geocodeData,
        CancellationToken cancellationToken)
    {
        var coordsResult = GeoCoordinates.Create(geocodeData.Latitude, geocodeData.Longitude);
        if (coordsResult.IsFailure) return;

        var geocodeResult = ShippingGeocode.Create(
            orderId,
            address,
            coordsResult.Value,
            geocodeData.PlaceType,
            geocodeData.RawResponse ?? string.Empty);

        if (geocodeResult.IsFailure) return;

        _geocodes.Upsert(geocodeResult.Value);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static OrderDto MapToDto(Order order, Dictionary<int, string>? productNames = null) => new()
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
            ProductName = productNames?.GetValueOrDefault(l.ProductId) ?? $"Product #{l.ProductId}",
            UnitPrice = l.UnitPrice.Amount,
            Quantity = l.Quantity,
            Discount = l.Discount,
            LineTotal = l.LineTotal.Amount
        }).ToList()
    };
}