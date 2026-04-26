namespace Northwind.Infrastructure.GoogleMaps;

/// <summary>
/// Strongly-typed configuration for Google Maps API integration.
/// Bound from the "GoogleMaps" section of appsettings.json.
/// </summary>
public sealed class GoogleMapsOptions
{
    public const string SectionName = "GoogleMaps";

    public string ApiKey { get; set; } = string.Empty;
    public string GeocodingEndpoint { get; set; } = "https://maps.googleapis.com/maps/api/geocode/json";
    public string StaticMapsEndpoint { get; set; } = "https://maps.googleapis.com/maps/api/staticmap";
}