using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the new ShippingGeocode entity to a brand-new ShippingGeocodes table.
/// This is the only table we create in the Northwind database — every other entity
/// maps to the existing legacy schema. Keeping geo-validation data in its own
/// table protects the legacy schema from upstream consumers.
/// </summary>
internal sealed class ShippingGeocodeConfiguration : IEntityTypeConfiguration<ShippingGeocode>
{
    public void Configure(EntityTypeBuilder<ShippingGeocode> builder)
    {
        builder.ToTable("ShippingGeocodes");

        builder.HasKey(g => g.Id);

        builder.Property(g => g.Id)
            .HasColumnType("uniqueidentifier");

        builder.Property(g => g.OrderId).IsRequired();

        builder.Property(g => g.PlaceType)
            .HasColumnType("nvarchar(50)")
            .IsRequired();

        builder.Property(g => g.RawResponse)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(g => g.ValidatedAt)
            .HasColumnType("datetime2");

        // The standardized address — same OwnsOne pattern as in OrderConfiguration,
        // but here the columns belong to ShippingGeocodes, not Orders.
        builder.OwnsOne(g => g.StandardizedAddress, address =>
        {
            address.Property(a => a.Street)
                .HasColumnName("Street")
                .HasColumnType("nvarchar(120)")
                .IsRequired();

            address.Property(a => a.City)
                .HasColumnName("City")
                .HasColumnType("nvarchar(50)")
                .IsRequired();

            address.Property(a => a.Region)
                .HasColumnName("Region")
                .HasColumnType("nvarchar(50)");

            address.Property(a => a.PostalCode)
                .HasColumnName("PostalCode")
                .HasColumnType("nvarchar(20)");

            address.Property(a => a.Country)
                .HasColumnName("Country")
                .HasColumnType("nvarchar(50)")
                .IsRequired();
        });

        // Coordinates — also a value object, owned by this entity.
        builder.OwnsOne(g => g.Coordinates, coords =>
        {
            coords.Property(c => c.Latitude)
                .HasColumnName("Latitude")
                .HasColumnType("float")
                .IsRequired();

            coords.Property(c => c.Longitude)
                .HasColumnName("Longitude")
                .HasColumnType("float")
                .IsRequired();
        });

        // Computed property — never persisted.
        builder.Ignore(g => g.IsAccessibleForHeavyFreight);

        // One-to-one relationship with Order. Cascade delete: if an order
        // is removed, its geocode goes with it.
        builder.HasOne<Order>()
            .WithOne()
            .HasForeignKey<ShippingGeocode>(g => g.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // OrderId is unique — one geocode per order. Index helps lookups by OrderId
        // (which is the most common query: "give me the geocode for order 1234").
        builder.HasIndex(g => g.OrderId).IsUnique();
    }
}