using Microsoft.EntityFrameworkCore;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository : IProductRepository
{
    private readonly NorthwindDbContext _db;

    public ProductRepository(NorthwindDbContext db) => _db = db;

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _db.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Product>> SearchByNameAsync(
        string fragment,
        int maxResults,
        CancellationToken cancellationToken = default)
        => await _db.Products
            .AsNoTracking()
            .Where(p => !p.Discontinued && p.ProductName.Contains(fragment))
            .OrderBy(p => p.ProductName)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
}