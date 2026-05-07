using System.Reflection;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.OpenApi;
using Northwind.Application.Orders.Validators;
using Northwind.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---- Validators (FluentValidation) ----
// Register all validators from the Application assembly and hook them into
// ASP.NET Core model binding so invalid requests are rejected early (400)
// before reaching the service layer.
builder.Services.AddValidatorsFromAssemblyContaining<CreateOrderCommandValidator>();
builder.Services.AddFluentValidationAutoValidation();

// ---- Services ----
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Northwind Order Management API",
        Version = "v1",
        Description = """
            REST API for managing Northwind Traders customer orders, shipments, and analytics.

            **Architecture**: Clean Architecture with 4 layers (Domain, Application, Infrastructure, API).

            **Validation**:
            - FluentValidation at the API boundary (400 Bad Request)
            - Domain/service rules inside Application (422 Unprocessable Entity)

            **Key features**:
            - Full order lifecycle: create → fulfill → ship
            - Google Maps address validation with geocoding cache
            - PDF invoice generation via QuestPDF
            - Analytics endpoints for dashboard KPIs
            """,
        Contact = new OpenApiContact
        {
            Name = "Carlos Puente",
            Url = new Uri("https://github.com/CarlosPuent/northwind-order-management")
        }
    });

    // Include XML documentation comments from the API project.
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);

    // Group endpoints by controller name for cleaner navigation.
    c.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] });
});

builder.Services.AddInfrastructure(builder.Configuration);

// ---- CORS ----
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? new[] { "http://localhost:9000" };

        policy.WithOrigins(origins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

var runningInContainer = string.Equals(
    Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"),
    "true",
    StringComparison.OrdinalIgnoreCase);

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Northwind Order Management v1");
    c.DocumentTitle = "Northwind API";
    c.DefaultModelsExpandDepth(-1); // Hide schemas section by default
    c.DisplayRequestDuration();     // Show request timing
});

app.UseRouting();
app.UseCors();

// In container scenarios the API typically runs HTTP-only; HTTPS redirection can break PUT/DELETE.
if (!runningInContainer)
    app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program
{
}