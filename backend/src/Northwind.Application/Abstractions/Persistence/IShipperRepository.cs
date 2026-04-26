using Northwind.Domain.Entities;

namespace Northwind.Application.Abstractions.Persistence;

/// <summary>
/// Read-only persistence contract for Shipper.
/// </summary>
public interface IShipperRepository
{
    Task<Shipper?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Returns all shippers (3 in Northwind), ordered by company name.</summary>
    Task<IReadOnlyList<Shipper>> GetAllAsync(CancellationToken cancellationToken = default);
}