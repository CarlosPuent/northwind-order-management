using Microsoft.EntityFrameworkCore;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Repositories;

internal sealed class ShipperRepository : IShipperRepository
{
    private readonly NorthwindDbContext _db;

    public ShipperRepository(NorthwindDbContext db) => _db = db;

    public async Task<Shipper?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _db.Shippers
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Shipper>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Shippers
            .AsNoTracking()
            .OrderBy(s => s.CompanyName)
            .ToListAsync(cancellationToken);
}