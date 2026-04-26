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
        // Remove any existing geocode for this order, then add the new one.
        // This maintains the one-geocode-per-order invariant at the persistence level.
        var existing = _db.ShippingGeocodes
            .Local
            .FirstOrDefault(g => g.OrderId == geocode.OrderId);

        if (existing != null)
            _db.ShippingGeocodes.Remove(existing);

        _db.ShippingGeocodes.Add(geocode);
    }
}