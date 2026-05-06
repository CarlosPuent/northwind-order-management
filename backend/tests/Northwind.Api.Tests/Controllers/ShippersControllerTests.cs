using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Northwind.Api.Controllers;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Domain.Entities;
using Xunit;

namespace Northwind.Api.Tests.Controllers;

public sealed class ShippersControllerTests
{
    private readonly Mock<IShipperRepository> _repo = new();
    private readonly ShippersController _sut;

    public ShippersControllerTests()
    {
        _sut = new ShippersController(_repo.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithProjectedShippers()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Shipper>
            {
                new(id: 1, companyName: "Speedy Express",   phone: "(503) 555-9831"),
                new(id: 2, companyName: "United Package",   phone: "(503) 555-3199"),
                new(id: 3, companyName: "Federal Shipping", phone: "(503) 555-9931")
            });

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
        items.Should().HaveCount(3);

        items[0].Should().BeEquivalentTo(new
        {
            Id = 1,
            CompanyName = "Speedy Express",
            Phone = "(503) 555-9831"
        });
    }

    [Fact]
    public async Task GetAll_WhenNoShippers_ShouldReturnOkWithEmptyList()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Shipper>());

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.Should().BeEmpty();
    }
}
