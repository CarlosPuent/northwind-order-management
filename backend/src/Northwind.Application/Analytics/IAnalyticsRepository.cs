namespace Northwind.Application.Analytics;

public interface IAnalyticsRepository
{
    Task<List<OrdersOverTimeDto>> GetOrdersOverTimeAsync(
        int? year, CancellationToken ct = default);

    Task<List<ShipmentsByRegionDto>> GetShipmentsByRegionAsync(
        int? year, CancellationToken ct = default);

    Task<List<TopCustomerDto>> GetTopCustomersAsync(
        int? year, int limit, CancellationToken ct = default);

    Task<List<int>> GetAvailableYearsAsync(CancellationToken ct = default);

    Task<List<DeliveryLocationDto>> GetDeliveryLocationsAsync(
        int limit, CancellationToken ct = default);
}
