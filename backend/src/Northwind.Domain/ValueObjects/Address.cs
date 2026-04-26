using Northwind.Domain.Common;

namespace Northwind.Domain.ValueObjects;

/// <summary>
/// A postal address structured into its component parts.
/// Used both for free-text input from the order form and for the standardized
/// address returned by Google Maps Address Validation API.
/// </summary>
/// <remarks>
/// All fields except <see cref="Region"/> and <see cref="PostalCode"/> are required —
/// some countries (e.g. Ireland, parts of UK) don't have postal codes; some
/// regions don't subdivide into states or provinces.
/// </remarks>
public sealed record Address
{
    public string Street { get; init; }
    public string City { get; init; }
    public string? Region { get; init; }
    public string? PostalCode { get; init; }
    public string Country { get; init; }

    // Public constructor required by EF Core. Use Create(...) in production code.
    public Address(string street, string city, string? region, string? postalCode, string country)
    {
        Street = street;
        City = city;
        Region = region;
        PostalCode = postalCode;
        Country = country;
    }

    /// <summary>
    /// Safely constructs an Address. Trims whitespace from every field and
    /// returns a failure Result if any required field is empty.
    /// </summary>
    public static Result<Address> Create(
        string street,
        string city,
        string? region,
        string? postalCode,
        string country)
    {
        if (string.IsNullOrWhiteSpace(street))
            return Error.Validation("Address.MissingStreet", "Street is required.");

        if (string.IsNullOrWhiteSpace(city))
            return Error.Validation("Address.MissingCity", "City is required.");

        if (string.IsNullOrWhiteSpace(country))
            return Error.Validation("Address.MissingCountry", "Country is required.");

        return new Address(
            street.Trim(),
            city.Trim(),
            string.IsNullOrWhiteSpace(region) ? null : region.Trim(),
            string.IsNullOrWhiteSpace(postalCode) ? null : postalCode.Trim(),
            country.Trim());
    }

    /// <summary>
    /// One-line representation, useful for display and for sending to
    /// Google Maps as a fallback when the structured form isn't available.
    /// </summary>
    public string ToSingleLine()
    {
        var parts = new List<string> { Street, City };
        if (!string.IsNullOrWhiteSpace(Region)) parts.Add(Region);
        if (!string.IsNullOrWhiteSpace(PostalCode)) parts.Add(PostalCode);
        parts.Add(Country);
        return string.Join(", ", parts);
    }

    public override string ToString() => ToSingleLine();
}