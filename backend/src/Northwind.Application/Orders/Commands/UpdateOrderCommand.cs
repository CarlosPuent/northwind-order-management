namespace Northwind.Application.Orders.Commands;

public sealed record UpdateOrderCommand(
    int OrderId,
    string CustomerId,
    int EmployeeId,
    DateTime OrderDate,
    int? ShipperId,
    string ShipName,
    string ShipStreet,
    string ShipCity,
    string? ShipRegion,
    string? ShipPostalCode,
    string ShipCountry,
    decimal Freight,
    List<OrderLineCommand> Lines,
    DateTime? RequiredDate = null,
    GeocodeCommandData? Geocode = null
);