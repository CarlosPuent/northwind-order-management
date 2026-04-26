using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Northwind.Domain.Entities;

namespace Northwind.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps the Employee domain entity to the legacy Employees table.
/// We deliberately ignore the columns we don't use (Photo, Notes, ReportsTo,
/// HireDate, etc.) — EF Core won't error on extra DB columns, and our domain
/// stays focused on what the system actually needs.
/// </summary>
internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("EmployeeID")
            .ValueGeneratedOnAdd();   // SQL IDENTITY column — DB assigns the value.

        builder.Property(e => e.FirstName)
            .HasColumnType("nvarchar(10)")
            .IsRequired();

        builder.Property(e => e.LastName)
            .HasColumnType("nvarchar(20)")
            .IsRequired();

        builder.Property(e => e.Title).HasColumnType("nvarchar(30)");
        builder.Property(e => e.City).HasColumnType("nvarchar(15)");
        builder.Property(e => e.Country).HasColumnType("nvarchar(15)");

        // FullName is a computed property in the domain — never persisted to DB.
        builder.Ignore(e => e.FullName);
    }
}