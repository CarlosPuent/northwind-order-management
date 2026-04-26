using Northwind.Domain.Common;
using Northwind.Domain.ValueObjects;

namespace Northwind.Application.Abstractions;

/// <summary>
/// Validates and geocodes a postal address using an external service (Google Maps).
/// Returns a standardized address, coordinates, and the place type.
/// </summary>
/// <remarks>
/// Application code depends on this interface. The real implementation
/// (GoogleMapsGeocodingService) and the cache decorator (CachedGeocodingService)
/// both live in Infrastructure — the domain and application layers never know
/// which one is active.
/// </remarks>
public interface IGeocodingService
{
    /// <summary>
    /// Validates the given address and returns geocoded coordinates.
    /// </summary>
    /// <param name="address">The address to validate and geocode.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A successful result with the geocoding response, or a failure with an error.</returns>
    Task<Result<GeocodingResult>> ValidateAndGeocodeAsync(
        Address address,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// The output of a geocoding operation. Carries the standardized address,
/// coordinates, place type, and the raw API response for auditability.
/// </summary>
public sealed record GeocodingResult(
    Address StandardizedAddress,
    GeoCoordinates Coordinates,
    string PlaceType,
    string RawResponse);
