using Northwind.Domain.Common;
using Northwind.Domain.ValueObjects;

namespace Northwind.Domain.Entities;

/// <summary>
/// Geo-validation result for an Order's shipping address. Stored in a separate
/// table (ShippingGeocodes) rather than as columns on Orders, to avoid altering
/// the legacy Northwind schema.
/// </summary>
/// <remarks>
/// One geocode per order (1-to-1 relationship). When the order's address changes,
/// the geocode is replaced rather than versioned — we keep the most recent
/// validated location only.
/// </remarks>
public sealed class ShippingGeocode : Entity<Guid>
{
    /// <summary>The Order this geocode belongs to.</summary>
    public int OrderId { get; private set; }

    /// <summary>The standardized address returned by Google Maps Address Validation.</summary>
    public Address StandardizedAddress { get; private set; }

    /// <summary>Latitude/longitude coordinates resolved by Google Maps.</summary>
    public GeoCoordinates Coordinates { get; private set; }

    /// <summary>
    /// The address type returned by Google (e.g. "premise", "street_address",
    /// "subpremise"). Used by the UI to derive the "DELIVERY STATUS" chip
    /// and decide whether the address is suitable for heavy freight.
    /// </summary>
    public string PlaceType { get; private set; }

    /// <summary>
    /// Raw JSON response from the Google Maps API for auditability and future use.
    /// Stored as nvarchar(max). If Google changes the response shape or we need
    /// a field we don't currently parse, we still have the original payload.
    /// </summary>
    public string RawResponse { get; private set; }

    /// <summary>UTC timestamp of when this geocode was produced.</summary>
    public DateTime ValidatedAt { get; private set; }

    private ShippingGeocode(
        Guid id,
        int orderId,
        Address standardizedAddress,
        GeoCoordinates coordinates,
        string placeType,
        string rawResponse,
        DateTime validatedAt) : base(id)
    {
        OrderId = orderId;
        StandardizedAddress = standardizedAddress;
        Coordinates = coordinates;
        PlaceType = placeType;
        RawResponse = rawResponse;
        ValidatedAt = validatedAt;
    }

    // Parameterless constructor for EF Core only.
    private ShippingGeocode() : base()
    {
        StandardizedAddress = null!;
        Coordinates = null!;
        PlaceType = string.Empty;
        RawResponse = string.Empty;
    }

    /// <summary>
    /// Factory method to create a ShippingGeocode from a Google Maps validation result.
    /// Generates a new Guid for the entity and stamps it with the current UTC time.
    /// </summary>
    public static Result<ShippingGeocode> Create(
        int orderId,
        Address standardizedAddress,
        GeoCoordinates coordinates,
        string placeType,
        string rawResponse)
    {
        if (orderId <= 0)
            return Error.Validation(
                "ShippingGeocode.InvalidOrderId",
                "Order id must be positive.");

        if (string.IsNullOrWhiteSpace(placeType))
            return Error.Validation(
                "ShippingGeocode.MissingPlaceType",
                "Place type is required.");

        // RawResponse can be empty if we ever need to seed test data manually,
        // but in normal flow it is the JSON Google returned.
        return new ShippingGeocode(
            Guid.NewGuid(),
            orderId,
            standardizedAddress,
            coordinates,
            placeType,
            rawResponse ?? string.Empty,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Returns true when the address resolves to a commercial/industrial
    /// premise suitable for heavy freight delivery. Used by the UI to render
    /// the "Accessible for Heavy Freight" chip.
    /// </summary>
    public bool IsAccessibleForHeavyFreight =>
        PlaceType.Equals("premise", StringComparison.OrdinalIgnoreCase) ||
        PlaceType.Equals("subpremise", StringComparison.OrdinalIgnoreCase) ||
        PlaceType.Equals("establishment", StringComparison.OrdinalIgnoreCase);
}