namespace Northwind.Application.Orders.Commands;

/// <summary>
/// Data required to update an existing order. Lines are replaced wholesale —
/// the client sends the full desired set of lines, not a diff.
/// </summary>
public sealed record UpdateOrderCommand(
    int OrderId,
    string CustomerId,
    int EmployeeId,
    int? ShipperId,
    string ShipName,
    string ShipStreet,
    string ShipCity,
    string? ShipRegion,
    string? ShipPostalCode,
    string ShipCountry,
    decimal Freight,
    List<OrderLineCommand> Lines);