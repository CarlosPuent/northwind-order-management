using AwesomeAssertions;
using Northwind.Domain.ValueObjects;
using Xunit;

namespace Northwind.Application.Tests.ValueObjects;

public class GeoCoordinatesTests
{
    [Fact]
    public void Create_WithValidCoordinates_ShouldSucceed()
    {
        var result = GeoCoordinates.Create(37.42, -122.08);

        result.IsSuccess.Should().BeTrue();
        result.Value.Latitude.Should().Be(37.42);
        result.Value.Longitude.Should().Be(-122.08);
    }

    [Theory]
    [InlineData(-91)]
    [InlineData(91)]
    public void Create_WithInvalidLatitude_ShouldFail(double lat)
    {
        var result = GeoCoordinates.Create(lat, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GeoCoordinates.InvalidLatitude");
    }

    [Theory]
    [InlineData(-181)]
    [InlineData(181)]
    public void Create_WithInvalidLongitude_ShouldFail(double lng)
    {
        var result = GeoCoordinates.Create(0, lng);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("GeoCoordinates.InvalidLongitude");
    }

    [Fact]
    public void ToMapsQuery_ShouldReturnCommaSeparatedLatLng()
    {
        var coords = new GeoCoordinates(37.42, -122.08);

        var query = coords.ToMapsQuery();

        query.Should().Be("37.42,-122.08");
    }

    [Fact]
    public void TwoCoordinates_WithSameValues_ShouldBeEqual()
    {
        var a = new GeoCoordinates(37.42, -122.08);
        var b = new GeoCoordinates(37.42, -122.08);

        a.Should().Be(b);
    }
}