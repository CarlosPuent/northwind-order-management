using Northwind.Domain.Common;

namespace Northwind.Domain.Entities;

/// <summary>
/// An employee of Northwind Traders, mapped to the Employees table.
/// In our system, employees are assigned to orders as the responsible salesperson.
/// </summary>
public sealed class Employee : Entity<int>
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string? Title { get; private set; }
    public string? City { get; private set; }
    public string? Country { get; private set; }

    /// <summary>Convenient computed property for display in dropdowns and reports.</summary>
    public string FullName => $"{FirstName} {LastName}";

    public Employee(
        int id,
        string firstName,
        string lastName,
        string? title,
        string? city,
        string? country) : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Title = title;
        City = city;
        Country = country;
    }

    // Parameterless constructor for EF Core only.
    private Employee() : base()
    {
        FirstName = string.Empty;
        LastName = string.Empty;
    }
}