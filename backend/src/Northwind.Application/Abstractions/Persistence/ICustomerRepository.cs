using Northwind.Domain.Entities;

namespace Northwind.Application.Abstractions.Persistence;

/// <summary>
/// Read-only persistence contract for Customer. Customers are managed by upstream
/// systems — our application reads them but doesn't create or modify them.
/// </summary>
public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all customers, ordered by company name. Used to populate the
    /// in Northwind) so we don't paginate here.
    /// </summary>
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches by company name fragment. Used by the order form's autocomplete.
    /// </summary>
    Task<IReadOnlyList<Customer>> SearchByNameAsync(
        string fragment,
        int maxResults,
        CancellationToken cancellationToken = default);
}