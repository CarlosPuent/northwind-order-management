using Northwind.Domain.Common;

namespace Northwind.Domain.Entities;

/// <summary>
/// A customer of Northwind Traders. Mapped to the existing Customers table
/// in the legacy Northwind schema.
/// </summary>
/// <remarks>
/// Northwind uses a 5-character string as the customer key (e.g. "ALFKI", "BERGS"),
/// not an integer. We honor that schema rather than trying to "fix" it — the table
/// is shared with downstream consumers we don't control.
/// </remarks>
public sealed class Customer : Entity<string>
{
    public string CompanyName { get; private set; }
    public string? ContactName { get; private set; }
    public string? ContactTitle { get; private set; }
    public string? City { get; private set; }
    public string? Region { get; private set; }
    public string? Country { get; private set; }
    public string? Phone { get; private set; }

    public Customer(
        string id,
        string companyName,
        string? contactName,
        string? contactTitle,
        string? city,
        string? region,
        string? country,
        string? phone) : base(id)
    {
        CompanyName = companyName;
        ContactName = contactName;
        ContactTitle = contactTitle;
        City = city;
        Region = region;
        Country = country;
        Phone = phone;
    }

    // Parameterless constructor for EF Core only.
    private Customer() : base()
    {
        CompanyName = string.Empty;
    }
}