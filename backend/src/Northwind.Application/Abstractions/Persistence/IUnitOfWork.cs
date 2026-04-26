namespace Northwind.Application.Abstractions.Persistence;

/// <summary>
/// Coordinates a single business transaction across one or more repositories.
/// Repositories track changes; the unit of work persists them in a single commit.
/// </summary>
/// <remarks>
/// In Clean Architecture, application services do not call repository.Save() —
/// they call unitOfWork.SaveChangesAsync() at the end of a use case. This makes
/// the transactional boundary explicit and lets EF Core batch DB round-trips.
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists all pending changes from any repository sharing this DbContext.
    /// Returns the number of rows affected.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}