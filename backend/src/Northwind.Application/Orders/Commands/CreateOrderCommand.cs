namespace Northwind.Application.Orders.Commands;

/// <summary>
/// Data required to create a new order. This is a CQRS-lite command —
/// a plain DTO that describes intent, not a full MediatR command.
/// Validation happens in OrderService, not in the DTO itself.
/// </summary>
public sealed record CreateOrderCommand(
    string CustomerId,
    int EmployeeId,
    DateTime OrderDate,
    string ShipName,
    string ShipStreet,
    string ShipCity,
    string? ShipRegion,
    string? ShipPostalCode,
    string ShipCountry,
    decimal Freight,
    List<OrderLineCommand> Lines);

/// <summary>
/// A single line item in a create/update order command.
/// </summary>
public sealed record OrderLineCommand(
    int ProductId,
    short Quantity,
    float Discount);