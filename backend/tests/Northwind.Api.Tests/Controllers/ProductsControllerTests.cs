using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Northwind.Api.Controllers;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Domain.Entities;
using Northwind.Domain.ValueObjects;
using Xunit;

namespace Northwind.Api.Tests.Controllers;

public sealed class ProductsControllerTests
{
    private readonly Mock<IProductRepository> _repo = new();
    private readonly ProductsController _sut;

    public ProductsControllerTests()
    {
        _sut = new ProductsController(_repo.Object);
    }

    // ---- Search ----

    [Fact]
    public async Task Search_WithValidQuery_ShouldReturnOk_WithProjectedProducts()
    {
        _repo.Setup(r => r.SearchByNameAsync("chai", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>
            {
                new(id: 1, productName: "Chai", unitPrice: new Money(18m, "USD"), discontinued: false, unitsInStock: 39)
            });

        var result = await _sut.Search("chai", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
        items.Should().ContainSingle();
        items[0].Should().BeEquivalentTo(new
        {
            Id = 1,
            ProductName = "Chai",
            UnitPrice = 18m,
            UnitsInStock = (short)39
        });
    }

    [Fact]
    public async Task Search_ShouldTrimQuery_BeforeCallingRepo()
    {
        _repo.Setup(r => r.SearchByNameAsync("chai", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Product>());

        await _sut.Search("  chai  ", CancellationToken.None);

        _repo.Verify(r => r.SearchByNameAsync("chai", 10, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_WithEmptyOrWhitespaceQuery_ShouldReturnEmpty_WithoutCallingRepo(string q)
    {
        var result = await _sut.Search(q, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.Should().BeEmpty();
        _repo.Verify(r => r.SearchByNameAsync(
            It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---- GetInitialProducts ----

    [Fact]
    public async Task GetInitialProducts_ShouldCallRepo_WithEmptyStringAndLimit50()
    {
        _repo.Setup(r => r.SearchByNameAsync("", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Product>());

        var result = await _sut.GetInitialProducts(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _repo.Verify(r => r.SearchByNameAsync("", 50, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetInitialProducts_ShouldReturnOk_WithProjectedProducts()
    {
        _repo.Setup(r => r.SearchByNameAsync("", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>
            {
                new(id: 1, productName: "Chai",  unitPrice: new Money(18m, "USD"), discontinued: false, unitsInStock: 39),
                new(id: 2, productName: "Chang", unitPrice: new Money(19m, "USD"), discontinued: false, unitsInStock: 17)
            });

        var result = await _sut.GetInitialProducts(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
        items.Should().HaveCount(2);
    }
}
