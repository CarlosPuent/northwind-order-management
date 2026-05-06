using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Northwind.Api.Controllers;
using Northwind.Application.Abstractions;
using Northwind.Domain.Common;
using Northwind.Domain.ValueObjects;
using Xunit;

namespace Northwind.Api.Tests.Controllers;

public sealed class GeocodingControllerTests
{
    private readonly Mock<IGeocodingService> _geocoding = new();
    private readonly GeocodingController _sut;

    public GeocodingControllerTests()
    {
        _sut = new GeocodingController(_geocoding.Object);
    }

    [Fact]
    public async Task Validate_WithEmptyStreet_ShouldReturn422_WithoutCallingGeocodingService()
    {
        var result = await _sut.Validate(
            street: "", city: "Berlin", country: "Germany",
            cancellationToken: CancellationToken.None);

        result.Should().BeOfType<UnprocessableEntityObjectResult>();
        _geocoding.Verify(g => g.ValidateAndGeocodeAsync(
            It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Validate_WithEmptyCity_ShouldReturn422_WithoutCallingGeocodingService()
    {
        var result = await _sut.Validate(
            street: "Unter den Linden 1", city: "", country: "Germany",
            cancellationToken: CancellationToken.None);

        result.Should().BeOfType<UnprocessableEntityObjectResult>();
        _geocoding.Verify(g => g.ValidateAndGeocodeAsync(
            It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Validate_WithEmptyCountry_ShouldReturn422_WithoutCallingGeocodingService()
    {
        var result = await _sut.Validate(
            street: "Unter den Linden 1", city: "Berlin", country: "",
            cancellationToken: CancellationToken.None);

        result.Should().BeOfType<UnprocessableEntityObjectResult>();
        _geocoding.Verify(g => g.ValidateAndGeocodeAsync(
            It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Validate_WhenGeocodingFails_ShouldReturn422()
    {
        _geocoding
            .Setup(g => g.ValidateAndGeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Validation("Geocoding.NotFound", "Address could not be geocoded."));

        var result = await _sut.Validate(
            street: "Fake Street 999", city: "Nowhere", country: "XX",
            cancellationToken: CancellationToken.None);

        result.Should().BeOfType<UnprocessableEntityObjectResult>();
    }

    [Fact]
    public async Task Validate_WhenGeocodingSucceeds_ShouldReturnOk_WithCoordinates()
    {
        var standardized = Address.Create("Unter den Linden 1", "Berlin", null, "10117", "Germany").Value;
        var coordinates = GeoCoordinates.Create(52.5166, 13.3806).Value;
        var geocodeResult = new GeocodingResult(standardized, coordinates, "premise", "{}");

        _geocoding
            .Setup(g => g.ValidateAndGeocodeAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(geocodeResult);

        var result = await _sut.Validate(
            street: "Unter den Linden 1", city: "Berlin", country: "Germany",
            postalCode: "10117", cancellationToken: CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }
}