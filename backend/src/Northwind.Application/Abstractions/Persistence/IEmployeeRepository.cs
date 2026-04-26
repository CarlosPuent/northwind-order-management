using Northwind.Domain.Entities;

namespace Northwind.Application.Abstractions.Persistence;

/// <summary>
/// Read-only persistence contract for Employee.
/// </summary>
public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all employees, ordered by last name. Used to populate the
    /// employee assignment dropdown in the order form.
    /// </summary>
    Task<IReadOnlyList<Employee>> GetAllAsync(CancellationToken cancellationToken = default);
}