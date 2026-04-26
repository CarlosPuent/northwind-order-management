using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the Customer domain entity to the legacy Customers table in Northwind.
/// </summary>
internal sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        // Map to the existing legacy table — name and column shapes match Northwind exactly.
        builder.ToTable("Customers");

        builder.HasKey(c => c.Id);

        // Northwind uses nchar(10), not nchar(5) as some scripts do. We honor the
        // actual schema and store padded codes; trimming happens at read time
        // via the value conversion below.
        builder.Property(c => c.Id)
            .HasColumnName("CustomerID")
            .HasColumnType("nchar(10)")
            .IsRequired()
            .HasConversion(
                domain => domain,                          // domain → DB: as-is
                db => db.TrimEnd());                       // DB → domain: trim trailing spaces

        builder.Property(c => c.CompanyName)
            .HasColumnType("nvarchar(40)")
            .IsRequired();

        builder.Property(c => c.ContactName).HasColumnType("nvarchar(30)");
        builder.Property(c => c.ContactTitle).HasColumnType("nvarchar(30)");
        builder.Property(c => c.City).HasColumnType("nvarchar(15)");
        builder.Property(c => c.Region).HasColumnType("nvarchar(15)");
        builder.Property(c => c.Country).HasColumnType("nvarchar(15)");
        builder.Property(c => c.Phone).HasColumnType("nvarchar(24)");
    }
}