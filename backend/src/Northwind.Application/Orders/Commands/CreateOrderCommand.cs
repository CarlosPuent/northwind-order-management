namespace Northwind.Application.Orders.Commands;

/// <summary>
/// Data passed from the geocoding validation result on the frontend.
/// Saved as a ShippingGeocode record when the order is created.
/// </summary>
public sealed record GeocodeCommandData(
    double Latitude,
    double Longitude,
    string PlaceType,
    string RawResponse
);

public sealed record OrderLineCommand(
    int ProductId,
    short Quantity,
    float Discount
);

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
    List<OrderLineCommand> Lines,
    DateTime? RequiredDate = null,
    // Optional: present when the user validated the address before submitting
    GeocodeCommandData? Geocode = null
);