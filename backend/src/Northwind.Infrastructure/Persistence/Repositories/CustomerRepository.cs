using Microsoft.EntityFrameworkCore;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Repositories;

internal sealed class CustomerRepository : ICustomerRepository
{
    private readonly NorthwindDbContext _db;

    public CustomerRepository(NorthwindDbContext db) => _db = db;

    public async Task<Customer?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        => await _db.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Customers
            .AsNoTracking()
            .OrderBy(c => c.CompanyName)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Customer>> SearchByNameAsync(
        string fragment,
        int maxResults,
        CancellationToken cancellationToken = default)
        => await _db.Customers
            .AsNoTracking()
            .Where(c => c.CompanyName.Contains(fragment))
            .OrderBy(c => c.CompanyName)
            .Take(maxResults)
            .ToListAsync(cancellationToken);
}