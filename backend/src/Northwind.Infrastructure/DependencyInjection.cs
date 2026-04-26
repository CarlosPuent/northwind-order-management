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

        // ---- Reporting ----
        services.AddScoped<IInvoiceGenerator, Northwind.Infrastructure.Reporting.QuestPdfInvoiceGenerator>();

        // ---- Google Maps ----
        services.Configure<GoogleMapsOptions>(opts =>
        {
            configuration.GetSection(GoogleMapsOptions.SectionName).Bind(opts);

            var envKey = Environment.GetEnvironmentVariable("GOOGLE_MAPS_API_KEY");
            if (!string.IsNullOrWhiteSpace(envKey))
            {
                opts.ApiKey = envKey;
            }
        });

        services.AddMemoryCache();

        services.AddHttpClient<GoogleMapsGeocodingService>();

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