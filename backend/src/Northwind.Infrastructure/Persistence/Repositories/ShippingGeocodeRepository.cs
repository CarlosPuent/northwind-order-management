using Microsoft.EntityFrameworkCore;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Repositories;

internal sealed class ShippingGeocodeRepository : IShippingGeocodeRepository
{
    private readonly NorthwindDbContext _db;

    public ShippingGeocodeRepository(NorthwindDbContext db) => _db = db;

    public async Task<ShippingGeocode?> GetByOrderIdAsync(
        int orderId,
        CancellationToken cancellationToken = default)
        => await _db.ShippingGeocodes
            .AsNoTracking()
            .FirstOrDefaultAsync(g => g.OrderId == orderId, cancellationToken);

    public void Upsert(ShippingGeocode geocode)
    {
        // True upsert:
        // - If a row already exists for this OrderId, update it in-place.
        // - Otherwise, insert a new row.
        // This prevents unique index violations on OrderId during order updates.
        var existing = _db.ShippingGeocodes
            .Local
            .FirstOrDefault(g => g.OrderId == geocode.OrderId)
            ?? _db.ShippingGeocodes.FirstOrDefault(g => g.OrderId == geocode.OrderId);

        if (existing is null)
        {
            _db.ShippingGeocodes.Add(geocode);
            return;
        }

        // Keep the existing Id (row identity), just replace the values.
        existing.Replace(
            geocode.StandardizedAddress,
            geocode.Coordinates,
            geocode.PlaceType,
            geocode.RawResponse,
            DateTime.UtcNow);
    }
}