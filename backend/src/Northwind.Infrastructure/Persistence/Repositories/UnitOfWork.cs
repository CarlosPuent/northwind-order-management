using Northwind.Application.Abstractions.Persistence;

namespace Northwind.Infrastructure.Persistence.Repositories;

/*<summary>
/// Thin wrapper around <see cref="NorthwindDbContext.SaveChangesAsync(CancellationToken)"/>.
/// Exists so application services depend on an abstraction, not on EF Core directly.
/// </summary>*/
internal sealed class UnitOfWork : IUnitOfWork
{
    private readonly NorthwindDbContext _db;

    public UnitOfWork(NorthwindDbContext db) => _db = db;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _db.SaveChangesAsync(cancellationToken);
}