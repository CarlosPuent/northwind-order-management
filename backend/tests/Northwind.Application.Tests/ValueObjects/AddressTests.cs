using AwesomeAssertions;
using Northwind.Domain.ValueObjects;
using Xunit;

namespace Northwind.Application.Tests.ValueObjects;

public class AddressTests
{
    [Fact]
    public void Create_WithValidInputs_ShouldSucceed()
    {
        var result = Address.Create("123 Main St", "NYC", "NY", "10001", "USA");

        result.IsSuccess.Should().BeTrue();
        result.Value.Street.Should().Be("123 Main St");
        result.Value.City.Should().Be("NYC");
        result.Value.Region.Should().Be("NY");
        result.Value.PostalCode.Should().Be("10001");
        result.Value.Country.Should().Be("USA");
    }

    [Fact]
    public void Create_WithMissingStreet_ShouldFail()
    {
        var result = Address.Create("", "NYC", null, null, "USA");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.MissingStreet");
    }

    [Fact]
    public void Create_WithMissingCity_ShouldFail()
    {
        var result = Address.Create("123 St", "", null, null, "USA");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.MissingCity");
    }

    [Fact]
    public void Create_WithMissingCountry_ShouldFail()
    {
        var result = Address.Create("123 St", "NYC", null, null, "");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.MissingCountry");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        var result = Address.Create("  123 Main St  ", "  NYC  ", "  NY  ", "  10001  ", "  USA  ");

        result.IsSuccess.Should().BeTrue();
        result.Value.Street.Should().Be("123 Main St");
        result.Value.City.Should().Be("NYC");
    }

    [Fact]
    public void Create_WithNullRegionAndPostalCode_ShouldStoreAsNull()
    {
        var result = Address.Create("123 St", "Dublin", null, null, "Ireland");

        result.IsSuccess.Should().BeTrue();
        result.Value.Region.Should().BeNull();
        result.Value.PostalCode.Should().BeNull();
    }

    [Fact]
    public void ToSingleLine_ShouldJoinNonEmptyParts()
    {
        var address = new Address("123 Main St", "NYC", "NY", "10001", "USA");

        address.ToSingleLine().Should().Be("123 Main St, NYC, NY, 10001, USA");
    }

    [Fact]
    public void ToSingleLine_WithNullRegion_ShouldSkipIt()
    {
        var address = new Address("123 Main St", "Dublin", null, null, "Ireland");

        address.ToSingleLine().Should().Be("123 Main St, Dublin, Ireland");
    }
}