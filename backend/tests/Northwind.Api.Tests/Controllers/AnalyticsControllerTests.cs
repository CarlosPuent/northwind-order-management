using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Northwind.Api.Controllers;
using Northwind.Application.Analytics;
using Xunit;

namespace Northwind.Api.Tests.Controllers;

public sealed class AnalyticsControllerTests
{
    private readonly Mock<IAnalyticsRepository> _repo = new();
    private readonly AnalyticsController _sut;

    public AnalyticsControllerTests()
    {
        _sut = new AnalyticsController(_repo.Object);
    }

    // ---- OrdersOverTime ----

    [Fact]
    public async Task OrdersOverTime_WithNoYear_ShouldReturnAllMonths()
    {
        _repo.Setup(r => r.GetOrdersOverTimeAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrdersOverTimeDto>
            {
                new(1997, 7, 2, 100m),
                new(1996, 7, 1, 65m)
            });

        var result = await _sut.OrdersOverTime(year: null, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task OrdersOverTime_WithYear_ShouldReturnOnlyThatYear()
    {
        _repo.Setup(r => r.GetOrdersOverTimeAsync(1997, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrdersOverTimeDto>
            {
                new(1997, 7, 2, 100m)
            });

        var result = await _sut.OrdersOverTime(year: 1997, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<List<OrdersOverTimeDto>>().Subject;
        list.Should().OnlyContain(x => x.Year == 1997);
    }

    [Fact]
    public async Task OrdersOverTime_WithYearWithNoOrders_ShouldReturnEmptyList()
    {
        _repo.Setup(r => r.GetOrdersOverTimeAsync(2099, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OrdersOverTimeDto>());

        var result = await _sut.OrdersOverTime(year: 2099, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<List<OrdersOverTimeDto>>()
            .Subject.Should().BeEmpty();
    }

    // ---- ShipmentsByRegion ----

    [Fact]
    public async Task ShipmentsByRegion_WithNoYear_ShouldReturnGroupedByCountry()
    {
        _repo.Setup(r => r.GetShipmentsByRegionAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShipmentsByRegionDto>
            {
                new("France", 1),
                new("Germany", 1),
                new("Brazil", 1)
            });

        var result = await _sut.ShipmentsByRegion(year: null, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ShipmentsByRegion_WithYear_ShouldFilterByYear()
    {
        _repo.Setup(r => r.GetShipmentsByRegionAsync(1997, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ShipmentsByRegionDto>
            {
                new("France", 1),
                new("Germany", 1)
            });

        var result = await _sut.ShipmentsByRegion(year: 1997, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ---- TopCustomers ----

    [Fact]
    public async Task TopCustomers_WithNoFilters_ShouldReturnDefaultLimit5()
    {
        _repo.Setup(r => r.GetTopCustomersAsync(null, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TopCustomerDto>
            {
                new("ALFKI", "Alfreds Futterkiste", 2, 165m),
                new("TOMSP", "Toms Spezialitäten", 1, 11m)
            });

        var result = await _sut.TopCustomers(year: null, limit: 5, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<List<TopCustomerDto>>().Subject;
        list.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task TopCustomers_WithYear_ShouldFilterByYear()
    {
        _repo.Setup(r => r.GetTopCustomersAsync(1997, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TopCustomerDto>
            {
                new("ALFKI", "Alfreds Futterkiste", 1, 100m),
                new("TOMSP", "Toms Spezialitäten", 1, 11m)
            });

        var result = await _sut.TopCustomers(year: 1997, limit: 5, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TopCustomers_WithLimitAbove20_ShouldClampTo20()
    {
        _repo.Setup(r => r.GetTopCustomersAsync(null, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<TopCustomerDto>());

        var result = await _sut.TopCustomers(year: null, limit: 999, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should().BeAssignableTo<List<TopCustomerDto>>().Subject;
        list.Count.Should().BeLessThanOrEqualTo(20);

        _repo.Verify(r => r.GetTopCustomersAsync(null, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- AvailableYears ----

    [Fact]
    public async Task AvailableYears_ShouldReturnDistinctYears_OrderedDescending()
    {
        _repo.Setup(r => r.GetAvailableYearsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1997, 1996 });

        var result = await _sut.AvailableYears(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var years = ok.Value.Should().BeAssignableTo<List<int>>().Subject;
        years.Should().BeInDescendingOrder();
        years.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task AvailableYears_WhenNoOrders_ShouldReturnEmptyList()
    {
        _repo.Setup(r => r.GetAvailableYearsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>());

        var result = await _sut.AvailableYears(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<List<int>>().Subject.Should().BeEmpty();
    }

    // ---- DeliveryLocations ----

    [Fact]
    public async Task DeliveryLocations_ShouldReturnOk()
    {
        _repo.Setup(r => r.GetDeliveryLocationsAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeliveryLocationDto>());

        var result = await _sut.DeliveryLocations(limit: 20, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeliveryLocations_WithLimitAbove50_ShouldClampTo50()
    {
        _repo.Setup(r => r.GetDeliveryLocationsAsync(50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DeliveryLocationDto>());

        var result = await _sut.DeliveryLocations(limit: 999, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();

        _repo.Verify(r => r.GetDeliveryLocationsAsync(50, It.IsAny<CancellationToken>()), Times.Once);
    }
}
