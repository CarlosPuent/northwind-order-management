using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Northwind.Application.Abstractions;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Infrastructure.GoogleMaps;
using Northwind.Infrastructure.Persistence;
using Northwind.Infrastructure.Persistence.Repositories;

namespace Northwind.Infrastructure;

/// <summary>
/// Extension method to register all Infrastructure services in one call.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ---- EF Core ----
        var connectionString = configuration.GetConnectionString("Northwind")
            ?? throw new InvalidOperationException("Connection string 'Northwind' is missing.");

        services.AddDbContext<NorthwindDbContext>(options =>
            options.UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly(typeof(NorthwindDbContext).Assembly.FullName);
            }));

        // ---- Repositories ----
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IShipperRepository, ShipperRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IShippingGeocodeRepository, ShippingGeocodeRepository>();

        // ---- Application Services ----
        services.AddScoped<Northwind.Application.Orders.OrderService>();

        // ---- Google Maps ----
        services.Configure<GoogleMapsOptions>(
            configuration.GetSection(GoogleMapsOptions.SectionName));

        services.AddMemoryCache();

        // Register the real geocoding service as a named/typed HttpClient.
        services.AddHttpClient<GoogleMapsGeocodingService>();

        // Register the decorator chain: CachedGeocodingService wraps GoogleMapsGeocodingService.
        // When anyone asks for IGeocodingService, they get the cached version.
        services.AddScoped<GoogleMapsGeocodingService>();
        services.AddScoped<IGeocodingService>(sp =>
        {
            var inner = sp.GetRequiredService<GoogleMapsGeocodingService>();
            var cache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CachedGeocodingService>>();
            return new CachedGeocodingService(inner, cache, logger);
        });

        return services;
    }
}