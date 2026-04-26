using Microsoft.EntityFrameworkCore;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository : IEmployeeRepository
{
    private readonly NorthwindDbContext _db;

    public EmployeeRepository(NorthwindDbContext db) => _db = db;

    public async Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        => await _db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _db.Employees
            .AsNoTracking()
            .OrderBy(e => e.LastName)
            .ToListAsync(cancellationToken);
}