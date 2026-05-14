namespace Northwind.Application.Analytics;

public sealed record OrdersOverTimeDto(int Year, int Month, int OrderCount, decimal TotalRevenue);

public sealed record ShipmentsByRegionDto(string Country, int OrderCount);

public sealed record TopCustomerDto(
    string CustomerId,
    string CompanyName,
    int OrderCount,
    decimal TotalRevenue);

public sealed record DeliveryLocationDto(
    int OrderId,
    double Latitude,
    double Longitude,
    string City,
    string Country,
    string PlaceType,
    DateTime ValidatedAt);
