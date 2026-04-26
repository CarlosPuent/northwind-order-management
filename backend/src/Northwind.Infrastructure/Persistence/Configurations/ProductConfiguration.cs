using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Domain.Entities;
using Northwind.Domain.ValueObjects;

namespace Northwind.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the Product domain entity to the legacy Products table.
/// Includes a value conversion from the Money value object to a plain decimal column,
/// which is what Northwind stores. Currency is hardcoded to USD because Northwind
/// has no currency column — every monetary value in the legacy schema is implicitly USD.
/// </summary>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("ProductID")
            .ValueGeneratedOnAdd();

        builder.Property(p => p.ProductName)
            .HasColumnType("nvarchar(40)")
            .IsRequired();

        // Money ↔ decimal value conversion. Northwind's UnitPrice is `money` SQL type.
        // We expose Money in the domain but only persist the decimal amount;
        // currency is implicit (USD) for the legacy schema.
        builder.Property(p => p.UnitPrice)
            .HasColumnName("UnitPrice")
            .HasColumnType("money")
            .HasConversion(
                domain => domain.Amount,                  // Money → decimal
                db => new Money(db, "USD"));              // decimal → Money

        builder.Property(p => p.Discontinued)
            .HasColumnType("bit")
            .IsRequired();

        builder.Property(p => p.UnitsInStock).HasColumnType("smallint");
    }
}