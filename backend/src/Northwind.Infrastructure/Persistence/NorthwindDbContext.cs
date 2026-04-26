using Microsoft.EntityFrameworkCore;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for the Northwind database. Maps the legacy Northwind tables
/// to our domain entities and adds the new ShippingGeocodes table for geo-validation.
/// </summary>
/// <remarks>
/// Configuration is split into one IEntityTypeConfiguration class per entity, kept in
/// the Configurations folder. This keeps the DbContext file small and the per-entity
/// mapping isolated and testable.
/// </remarks>
public sealed class NorthwindDbContext : DbContext
{
    public NorthwindDbContext(DbContextOptions<NorthwindDbContext> options) : base(options)
    {
    }

    // ----- DbSets — one per aggregate / entity exposed to the rest of the system -----

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Employee> Employees => Set<Employee>();
    public DbSet<Shipper> Shippers => Set<Shipper>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<ShippingGeocode> ShippingGeocodes => Set<ShippingGeocode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply every IEntityTypeConfiguration found in this assembly.
        // No need to remember adding each one by hand — EF Core scans for them.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NorthwindDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}