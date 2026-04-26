using Northwind.Domain.Common;

namespace Northwind.Domain.ValueObjects;

/// <summary>
/// A point on Earth, represented by latitude and longitude in decimal degrees.
/// Used to store the geocoded location of an order's shipping address.
/// </summary>
/// <remarks>
/// Latitude must be between -90 and 90; longitude between -180 and 180.
/// Construct via <see cref="Create"/> to enforce these ranges.
/// </remarks>
public sealed record GeoCoordinates
{
    /// <summary>Latitude in decimal degrees. Range: [-90, 90].</summary>
    public double Latitude { get; init; }

    /// <summary>Longitude in decimal degrees. Range: [-180, 180].</summary>
    public double Longitude { get; init; }

    // Public constructor required by EF Core. Use Create(...) in production code.
    public GeoCoordinates(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    /// <summary>
    /// Safely constructs GeoCoordinates. Returns a failure Result if the values
    /// fall outside the valid ranges for Earth coordinates.
    /// </summary>
    public static Result<GeoCoordinates> Create(double latitude, double longitude)
    {
        if (latitude is < -90 or > 90)
            return Error.Validation(
                "GeoCoordinates.InvalidLatitude",
                $"Latitude {latitude} is out of range. Must be between -90 and 90.");

        if (longitude is < -180 or > 180)
            return Error.Validation(
                "GeoCoordinates.InvalidLongitude",
                $"Longitude {longitude} is out of range. Must be between -180 and 180.");

        return new GeoCoordinates(latitude, longitude);
    }

    /// <summary>
    /// Returns the canonical "lat,lng" string used by Google Maps Static Maps API.
    /// Uses invariant culture so the decimal separator is always a dot, never a comma —
    /// a culture-specific format here would silently break the API call.
    /// </summary>
    public string ToMapsQuery() =>
        $"{Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}," +
        $"{Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}";

    public override string ToString() => $"({Latitude:F6}, {Longitude:F6})";
}