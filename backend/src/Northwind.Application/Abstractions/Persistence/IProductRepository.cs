using Northwind.Domain.Entities;

namespace Northwind.Application.Abstractions.Persistence;

/// <summary>
/// Read-only persistence contract for Product. Used to populate order lines.
/// </summary>
public interface IProductRepository
{
    Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches active (non-discontinued) products by name fragment. Used by
    /// the order form's product autocomplete.
    /// </summary>
    Task<IReadOnlyList<Product>> SearchByNameAsync(
        string fragment,
        int maxResults,
        CancellationToken cancellationToken = default);
}