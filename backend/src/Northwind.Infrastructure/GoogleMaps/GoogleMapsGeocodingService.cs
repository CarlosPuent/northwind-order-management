using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Northwind.Application.Abstractions;
using Northwind.Domain.Common;
using Northwind.Domain.ValueObjects;

namespace Northwind.Infrastructure.GoogleMaps;

/// <summary>
/// Calls Google Maps Geocoding API to validate an address and return coordinates.
/// This is the "real" implementation — it makes HTTP calls on every invocation.
/// In production, it is wrapped by <see cref="CachedGeocodingService"/> so repeated
/// lookups for the same address don't cost money or latency.
/// </summary>
internal sealed class GoogleMapsGeocodingService : IGeocodingService
{
    private readonly HttpClient _http;
    private readonly GoogleMapsOptions _options;
    private readonly ILogger<GoogleMapsGeocodingService> _logger;

    public GoogleMapsGeocodingService(
        HttpClient http,
        IOptions<GoogleMapsOptions> options,
        ILogger<GoogleMapsGeocodingService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Result<GeocodingResult>> ValidateAndGeocodeAsync(
        Address address,
        CancellationToken cancellationToken = default)
    {
        var addressString = address.ToSingleLine();

        var url = $"{_options.GeocodingEndpoint}?address={Uri.EscapeDataString(addressString)}&key={_options.ApiKey}";

        _logger.LogInformation("Geocoding address: {Address}", addressString);

        HttpResponseMessage response;
        try
        {
            response = await _http.GetAsync(url, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google Maps API call failed for address: {Address}", addressString);
            return Error.Failure(
                "Geocoding.ApiError",
                "Failed to reach the Google Maps API. Please try again later.");
        }

        var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Google Maps returned {StatusCode} for: {Address}", response.StatusCode, addressString);
            return Error.Failure(
                "Geocoding.ApiError",
                $"Google Maps API returned status {(int)response.StatusCode}.");
        }

        // Parse the JSON response.
        using var doc = JsonDocument.Parse(rawJson);
        var root = doc.RootElement;

        var status = root.GetProperty("status").GetString();
        if (status != "OK")
        {
            _logger.LogWarning("Geocoding status '{Status}' for: {Address}", status, addressString);
            return Error.Validation(
                "Geocoding.NoResults",
                $"Google Maps could not validate this address. Status: {status}");
        }

        var firstResult = root.GetProperty("results")[0];

        // Extract coordinates.
        var location = firstResult.GetProperty("geometry").GetProperty("location");
        var lat = location.GetProperty("lat").GetDouble();
        var lng = location.GetProperty("lng").GetDouble();

        var coordsResult = GeoCoordinates.Create(lat, lng);
        if (coordsResult.IsFailure)
            return coordsResult.Error;

        // Extract formatted address parts.
        var formattedAddress = firstResult.GetProperty("formatted_address").GetString() ?? addressString;

        // Extract place type (first type from the types array).
        var types = firstResult.GetProperty("types");
        var placeType = types.GetArrayLength() > 0
            ? types[0].GetString() ?? "unknown"
            : "unknown";

        // Build a standardized address from the formatted result.
        // We use the original address fields but could parse address_components for precision.
        var standardized = new Address(
            address.Street,
            address.City,
            address.Region,
            address.PostalCode,
            address.Country);

        var geocodingResult = new GeocodingResult(
            standardized,
            coordsResult.Value,
            placeType,
            rawJson);

        _logger.LogInformation(
            "Geocoded '{Address}' → ({Lat}, {Lng}), type={PlaceType}",
            addressString, lat, lng, placeType);

        return geocodingResult;
    }
}