using Northwind.Domain.Common;

namespace Northwind.Domain.Entities;

/// <summary>
/// A shipping company used to deliver orders. Mapped to the Shippers table.
/// Each order is assigned to exactly one Shipper at creation time.
/// </summary>
public sealed class Shipper : Entity<int>
{
    public string CompanyName { get; private set; }
    public string? Phone { get; private set; }

    public Shipper(int id, string companyName, string? phone) : base(id)
    {
        CompanyName = companyName;
        Phone = phone;
    }

    // Parameterless constructor for EF Core only.
    private Shipper() : base()
    {
        CompanyName = string.Empty;
    }
}