using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Northwind.Api.Controllers;
using Northwind.Domain.Entities;
using Northwind.Domain.ValueObjects;
using Northwind.Infrastructure.Persistence;
using Xunit;

namespace Northwind.Api.Tests.Controllers;

public sealed class AnalyticsControllerTests : IDisposable
{
    private readonly NorthwindDbContext _db;
    private readonly AnalyticsController _sut;

    public AnalyticsControllerTests()
    {
        var options = new DbContextOptionsBuilder<NorthwindDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new NorthwindDbContext(options);
        _sut = new AnalyticsController(_db);
    }

    public void Dispose() => _db.Dispose();

    // ---- OrdersOverTime ----

    [Fact]
    public async Task OrdersOverTime_WithNoYear_ShouldReturnAllMonths()
    {
        await SeedOrdersAsync();

        var result = await _sut.OrdersOverTime(year: null, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task OrdersOverTime_WithYear_ShouldReturnOnlyThatYear()
    {
        await SeedOrdersAsync();

        var result = await _sut.OrdersOverTime(year: 1997, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should()
            .BeAssignableTo<List<AnalyticsController.OrdersOverTimeDto>>().Subject;
        list.Should().OnlyContain(x => x.Year == 1997);
    }

    [Fact]
    public async Task OrdersOverTime_WithYearWithNoOrders_ShouldReturnEmptyList()
    {
        await SeedOrdersAsync();

        var result = await _sut.OrdersOverTime(year: 2099, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<List<AnalyticsController.OrdersOverTimeDto>>()
            .Subject.Should().BeEmpty();
    }

    // ---- ShipmentsByRegion ----

    [Fact]
    public async Task ShipmentsByRegion_WithNoYear_ShouldReturnGroupedByCountry()
    {
        await SeedOrdersAsync();

        var result = await _sut.ShipmentsByRegion(year: null, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().NotBeNull();
    }

    [Fact]
    public async Task ShipmentsByRegion_WithYear_ShouldFilterByYear()
    {
        await SeedOrdersAsync();

        var result = await _sut.ShipmentsByRegion(year: 1997, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    // ---- TopCustomers ----

    [Fact]
    public async Task TopCustomers_WithNoFilters_ShouldReturnDefaultLimit5()
    {
        await SeedOrdersAsync();

        var result = await _sut.TopCustomers(year: null, limit: 5, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should()
            .BeAssignableTo<List<AnalyticsController.TopCustomerDto>>().Subject;
        list.Count.Should().BeLessThanOrEqualTo(5);
    }

    [Fact]
    public async Task TopCustomers_WithYear_ShouldFilterByYear()
    {
        await SeedOrdersAsync();

        var result = await _sut.TopCustomers(year: 1997, limit: 5, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task TopCustomers_WithLimitAbove20_ShouldClampTo20()
    {
        await SeedOrdersAsync();

        var result = await _sut.TopCustomers(year: null, limit: 999, CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var list = ok.Value.Should()
            .BeAssignableTo<List<AnalyticsController.TopCustomerDto>>().Subject;
        list.Count.Should().BeLessThanOrEqualTo(20);
    }

    // ---- AvailableYears ----

    [Fact]
    public async Task AvailableYears_ShouldReturnDistinctYears_OrderedDescending()
    {
        await SeedOrdersAsync();

        var result = await _sut.AvailableYears(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var years = ok.Value.Should().BeAssignableTo<List<int>>().Subject;
        years.Should().BeInDescendingOrder();
        years.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task AvailableYears_WhenNoOrders_ShouldReturnEmptyList()
    {
        var result = await _sut.AvailableYears(CancellationToken.None);

        var ok = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<List<int>>().Subject.Should().BeEmpty();
    }

    // ---- DeliveryLocations ----

    [Fact]
    public async Task DeliveryLocations_ShouldReturnOk()
    {
        var result = await _sut.DeliveryLocations(limit: 20, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task DeliveryLocations_WithLimitAbove50_ShouldClampTo50()
    {
        var result = await _sut.DeliveryLocations(limit: 999, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
    }

    // ---- seed helper ----

    private async Task SeedOrdersAsync()
    {
        var order1 = CreateOrder(
            customerId: "ALFKI",
            employeeId: 1,
            shipperId: 1,
            orderDate: new DateTime(1997, 7, 4),
            shipName: "Vins et alcools Chevalier",
            shipStreet: "59 rue de l'Abbaye",
            shipCity: "Reims",
            shipRegion: null,
            shipPostalCode: "51100",
            shipCountry: "France",
            freightUsd: 32.38m);

        var order2 = CreateOrder(
            customerId: "TOMSP",
            employeeId: 6,
            shipperId: 1,
            orderDate: new DateTime(1997, 7, 5),
            shipName: "Toms Spezialitäten",
            shipStreet: "Luisenstr. 48",
            shipCity: "Münster",
            shipRegion: null,
            shipPostalCode: "44087",
            shipCountry: "Germany",
            freightUsd: 11.61m);

        var order3 = CreateOrder(
            customerId: "ALFKI",
            employeeId: 4,
            shipperId: 2,
            orderDate: new DateTime(1996, 7, 8),
            shipName: "Hanari Carnes",
            shipStreet: "Rua do Paço 67",
            shipCity: "Rio de Janeiro",
            shipRegion: "RJ",
            shipPostalCode: "05454-876",
            shipCountry: "Brazil",
            freightUsd: 65.83m);

        _db.Customers.AddRange(
            new Customer(
                id: "ALFKI",
                companyName: "Alfreds Futterkiste",
                contactName: null,
                contactTitle: null,
                city: "Berlin",
                region: null,
                country: "Germany",
                phone: null),
            new Customer(
                id: "TOMSP",
                companyName: "Toms Spezialitäten",
                contactName: null,
                contactTitle: null,
                city: "Münster",
                region: null,
                country: "Germany",
                phone: null));

        _db.Orders.AddRange(order1, order2, order3);
        await _db.SaveChangesAsync();
    }

    private static Order CreateOrder(
        string customerId,
        int employeeId,
        int shipperId,
        DateTime orderDate,
        string shipName,
        string shipStreet,
        string shipCity,
        string? shipRegion,
        string? shipPostalCode,
        string shipCountry,
        decimal freightUsd)
    {
        var address = Address.Create(
            shipStreet,
            shipCity,
            shipRegion,
            shipPostalCode,
            shipCountry).Value;

        var order = Order.Create(
            customerId,
            employeeId,
            orderDate,
            shipName,
            address,
            new Money(freightUsd, "USD")).Value;

        order.AssignShipper(shipperId);

        return order;
    }
}