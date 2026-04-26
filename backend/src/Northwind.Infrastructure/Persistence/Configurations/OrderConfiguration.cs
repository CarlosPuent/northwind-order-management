using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Domain.Entities;
using Northwind.Domain.ValueObjects;

namespace Northwind.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the Order aggregate to the legacy Orders table. The complex part is mapping
/// the Address value object across multiple legacy columns (Ship*, ShipCity, etc.)
/// and the OrderLine collection to the [Order Details] table with its composite key.
/// </summary>
internal sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("OrderID")
            .ValueGeneratedOnAdd();

        // Customer FK (nchar(10), padded — same trimming approach as in CustomerConfiguration).
        builder.Property(o => o.CustomerId)
            .HasColumnName("CustomerID")
            .HasColumnType("nchar(10)")
            .HasConversion(
                domain => domain,
                db => db.TrimEnd());

        builder.Property(o => o.EmployeeId).HasColumnName("EmployeeID");

        // Northwind names this column ShipVia, but in our domain it's the more
        // meaningful ShipperId. The mapping makes the API contract clean
        // without renaming the legacy column.
        builder.Property(o => o.ShipperId).HasColumnName("ShipVia");

        builder.Property(o => o.OrderDate).HasColumnType("datetime");
        builder.Property(o => o.RequiredDate).HasColumnType("datetime");
        builder.Property(o => o.ShippedDate).HasColumnType("datetime");

        // Freight: Money ↔ decimal conversion. Northwind has no currency column,
        // so we hardcode USD.
        builder.Property(o => o.Freight)
            .HasColumnName("Freight")
            .HasColumnType("money")
            .HasConversion(
                domain => domain.Amount,
                db => new Money(db, "USD"));

        builder.Property(o => o.ShipName)
            .HasColumnName("ShipName")
            .HasColumnType("nvarchar(40)");

        // Address as an "owned entity" — multiple DB columns map to a single
        // value object in the domain. From the outside, code reads order.ShipAddress.City;
        // EF Core knows to read it from the ShipCity column.
        builder.OwnsOne(o => o.ShipAddress, address =>
        {
            address.Property(a => a.Street)
                .HasColumnName("ShipAddress")
                .HasColumnType("nvarchar(60)");

            address.Property(a => a.City)
                .HasColumnName("ShipCity")
                .HasColumnType("nvarchar(15)");

            address.Property(a => a.Region)
                .HasColumnName("ShipRegion")
                .HasColumnType("nvarchar(15)");

            address.Property(a => a.PostalCode)
                .HasColumnName("ShipPostalCode")
                .HasColumnType("nvarchar(10)");

            address.Property(a => a.Country)
                .HasColumnName("ShipCountry")
                .HasColumnType("nvarchar(15)");
        });

        // Computed properties — never persisted.
        builder.Ignore(o => o.IsShipped);
        builder.Ignore(o => o.IsEditable);
        builder.Ignore(o => o.SubTotal);
        builder.Ignore(o => o.Total);

        // Order owns its lines. EF Core configures the OrderLine relationship here
        // (via OwnsMany) so external code can never query OrderLines directly —
        // the only way to get them is through Order.Lines.
        builder.OwnsMany(o => o.Lines, lines =>
        {
            lines.ToTable("Order Details");

            // The OrderID FK column on Order Details — explicit shadow property
            // with type, so EF Core knows it's an int.
            lines.WithOwner().HasForeignKey("OrderID");
            lines.Property<int>("OrderID").HasColumnName("OrderID");

            // ProductId is a real CLR property on OrderLine; map the column name explicitly.
            lines.Property(l => l.ProductId).HasColumnName("ProductID");

            // Composite key uses real property expressions, not strings —
            // strings would create shadow properties with no defined type.
            lines.HasKey("OrderID", nameof(OrderLine.ProductId));

            lines.Property(l => l.UnitPrice)
                .HasColumnName("UnitPrice")
                .HasColumnType("money")
                .HasConversion(
                    domain => domain.Amount,
                    db => new Money(db, "USD"));

            lines.Property(l => l.Quantity).HasColumnType("smallint");
            lines.Property(l => l.Discount).HasColumnType("real");

            // LineTotal is computed in the domain — never stored.
            lines.Ignore(l => l.LineTotal);

            // OrderLine inherits Id from Entity<int>, but Northwind's Order Details
            // doesn't have a surrogate Id column — the composite key is the real PK.
            lines.Ignore(l => l.Id);

            // OrderId on OrderLine is informational; the actual FK is the shadow "OrderID" above.
            lines.Ignore(l => l.OrderId);
        });
    }
}