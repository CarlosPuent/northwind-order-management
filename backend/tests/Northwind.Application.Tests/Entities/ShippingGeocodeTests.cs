using AwesomeAssertions;
using Northwind.Domain.Entities;
using Northwind.Domain.ValueObjects;
using Xunit;

namespace Northwind.Application.Tests.Entities;

public class ShippingGeocodeTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldSucceed()
    {
        var address = new Address("123 Main St", "NYC", "NY", "10001", "USA");
        var coords = new GeoCoordinates(40.7128, -74.0060);

        var result = ShippingGeocode.Create(1, address, coords, "premise", "{}");

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().Be(1);
        result.Value.PlaceType.Should().Be("premise");
    }

    [Fact]
    public void Create_WithInvalidOrderId_ShouldFail()
    {
        var address = new Address("123 Main St", "NYC", "NY", "10001", "USA");
        var coords = new GeoCoordinates(40.7128, -74.0060);

        var result = ShippingGeocode.Create(0, address, coords, "premise", "{}");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShippingGeocode.InvalidOrderId");
    }

    [Fact]
    public void Create_WithEmptyPlaceType_ShouldFail()
    {
        var address = new Address("123 Main St", "NYC", "NY", "10001", "USA");
        var coords = new GeoCoordinates(40.7128, -74.0060);

        var result = ShippingGeocode.Create(1, address, coords, "", "{}");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ShippingGeocode.MissingPlaceType");
    }

    [Theory]
    [InlineData("premise", true)]
    [InlineData("subpremise", true)]
    [InlineData("establishment", true)]
    [InlineData("street_address", false)]
    [InlineData("route", false)]
    public void IsAccessibleForHeavyFreight_ShouldDependOnPlaceType(string placeType, bool expected)
    {
        var address = new Address("123 Main St", "NYC", "NY", "10001", "USA");
        var coords = new GeoCoordinates(40.7128, -74.0060);
        var geocode = ShippingGeocode.Create(1, address, coords, placeType, "{}").Value;

        geocode.IsAccessibleForHeavyFreight.Should().Be(expected);
    }
}