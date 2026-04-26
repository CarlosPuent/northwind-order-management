using Northwind.Domain.Common;
using Northwind.Domain.ValueObjects;

namespace Northwind.Domain.Entities;

/// <summary>
/// A product sold by Northwind Traders. Used as a line item in orders.
/// Maps to the Products table in the legacy schema.
/// </summary>
/// <remarks>
/// We expose UnitPrice as a strongly-typed Money value rather than a raw decimal.
/// Northwind stores it as a plain decimal in USD; the EF Core configuration
/// converts between the two representations.
/// </remarks>
public sealed class Product : Entity<int>
{
    public string ProductName { get; private set; }
    public Money UnitPrice { get; private set; }
    public bool Discontinued { get; private set; }
    public short? UnitsInStock { get; private set; }

    public Product(
        int id,
        string productName,
        Money unitPrice,
        bool discontinued,
        short? unitsInStock) : base(id)
    {
        ProductName = productName;
        UnitPrice = unitPrice;
        Discontinued = discontinued;
        UnitsInStock = unitsInStock;
    }

    // Parameterless constructor for EF Core only.
    private Product() : base()
    {
        ProductName = string.Empty;
        UnitPrice = Money.Zero;
    }
}