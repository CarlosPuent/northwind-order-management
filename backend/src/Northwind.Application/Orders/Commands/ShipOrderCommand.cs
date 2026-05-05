namespace Northwind.Application.Orders.Commands;

/// <summary>
/// Marks an order as shipped. Requires a shipper to be assigned.
/// Sets ShippedDate which is the source of truth for IsShipped.
/// </summary>
public sealed record ShipOrderCommand(
    int OrderId,
    int ShipperId,
    DateTime? ShippedDate = null
);