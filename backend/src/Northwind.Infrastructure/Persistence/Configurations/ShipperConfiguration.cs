using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the Shipper domain entity to the legacy Shippers table.
/// </summary>
internal sealed class ShipperConfiguration : IEntityTypeConfiguration<Shipper>
{
    public void Configure(EntityTypeBuilder<Shipper> builder)
    {
        builder.ToTable("Shippers");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("ShipperID")
            .ValueGeneratedOnAdd();

        builder.Property(s => s.CompanyName)
            .HasColumnType("nvarchar(40)")
            .IsRequired();

        builder.Property(s => s.Phone).HasColumnType("nvarchar(24)");
    }
}