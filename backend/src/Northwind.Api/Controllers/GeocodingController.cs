using Microsoft.AspNetCore.Mvc;
using Northwind.Application.Abstractions;
using Northwind.Domain.Common;
using Northwind.Domain.ValueObjects;

namespace Northwind.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class GeocodingController : ControllerBase
{
    private readonly IGeocodingService _geocoding;

    public GeocodingController(IGeocodingService geocoding)
    {
        _geocoding = geocoding;
    }

    /// <summary>
    /// Validates an address and returns geocoded coordinates.
    /// GET /api/geocoding/validate?street=1600+Amphitheatre+Parkway&amp;city=Mountain+View&amp;country=US
    /// </summary>
    [HttpGet("validate")]
    public async Task<IActionResult> Validate(
        [FromQuery] string street,
        [FromQuery] string city,
        [FromQuery] string country,
        [FromQuery] string? region = null,
        [FromQuery] string? postalCode = null,
        CancellationToken cancellationToken = default)
    {
        var addressResult = Address.Create(street, city, region, postalCode, country);
        if (addressResult.IsFailure)
            return UnprocessableEntity(new { error = addressResult.Error.Message });

        var result = await _geocoding.ValidateAndGeocodeAsync(addressResult.Value, cancellationToken);

        if (result.IsFailure)
            return UnprocessableEntity(new { error = result.Error.Message });

        var geocode = result.Value;
        return Ok(new
        {
            standardizedAddress = geocode.StandardizedAddress.ToSingleLine(),
            latitude = geocode.Coordinates.Latitude,
            longitude = geocode.Coordinates.Longitude,
            placeType = geocode.PlaceType,
            mapsQuery = geocode.Coordinates.ToMapsQuery()
        });
    }
}