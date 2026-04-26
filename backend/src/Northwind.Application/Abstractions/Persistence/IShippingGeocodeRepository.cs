using Northwind.Domain.Entities;

namespace Northwind.Application.Abstractions.Persistence;

/// <summary>
/// Persistence contract for ShippingGeocode entries (the new ShippingGeocodes table).
/// </summary>
public interface IShippingGeocodeRepository
{
    /// <summary>Returns the geocode for a given order, or null if not yet validated.</summary>
    Task<ShippingGeocode?> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Stages a new geocode for insertion, replacing any existing one for the same order.
    /// One geocode per order is the invariant; the unique index on OrderId enforces it.
    /// </summary>
    void Upsert(ShippingGeocode geocode);
}