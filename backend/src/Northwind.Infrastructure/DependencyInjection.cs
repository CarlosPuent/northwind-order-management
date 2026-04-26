using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Infrastructure.Persistence;
using Northwind.Infrastructure.Persistence.Repositories;

namespace Northwind.Infrastructure;

/// <summary>
/// Extension method to register all Infrastructure services in one call.
/// This is the only public surface of the Infrastructure project — everything
/// else (repositories, DbContext config) is internal.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Northwind")
            ?? throw new InvalidOperationException("Connection string 'Northwind' is missing.");

        services.AddDbContext<NorthwindDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(NorthwindDbContext).Assembly.FullName);
            }));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IShipperRepository, ShipperRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IShippingGeocodeRepository, ShippingGeocodeRepository>();

        return services;
    }
}