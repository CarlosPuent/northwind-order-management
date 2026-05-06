using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Northwind.Api.Controllers;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Domain.Entities;
using Xunit;

namespace Northwind.Api.Tests.Controllers;

public sealed class CustomersControllerTests
{
    private readonly Mock<ICustomerRepository> _repo = new();
    private readonly CustomersController _sut;

    public CustomersControllerTests()
    {
        _sut = new CustomersController(_repo.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithProjectedCustomers()
    {
        var customers = new List<Customer>
        {
            CreateCustomer(id: "ALFKI", companyName: "Alfreds", contactName: "Maria", city: "Berlin", country: "Germany"),
            CreateCustomer(id: "BOLID", companyName: "Bolido", contactName: "Martin", city: "Madrid", country: "Spain", region: "MD", phone: "+34 555")
        };

        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
        items.Should().HaveCount(2);

        items[0].Should().BeEquivalentTo(new
        {
            Id = "ALFKI",
            CompanyName = "Alfreds",
            ContactName = "Maria",
            City = "Berlin",
            Region = (string?)null,
            Country = "Germany",
            Phone = (string?)null
        });

        items[1].Should().BeEquivalentTo(new
        {
            Id = "BOLID",
            CompanyName = "Bolido",
            ContactName = "Martin",
            City = "Madrid",
            Region = "MD",
            Country = "Spain",
            Phone = "+34 555"
        });
    }

    [Fact]
    public async Task GetAll_WhenNoCustomers_ShouldReturnOkWithEmptyList()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Customer>());

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.Should().BeEmpty();
    }

    [Fact]
    public async Task Search_WithValidQuery_ShouldReturnOk_WithProjectedCustomers()
    {
        _repo.Setup(r => r.SearchByNameAsync("Alf", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>
            {
                CreateCustomer(id: "ALFKI", companyName: "Alfreds", contactName: "Maria", city: "Berlin", country: "Germany")
            });

        var result = await _sut.Search("Alf", CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
        items.Should().ContainSingle();
        items[0].Should().BeEquivalentTo(new
        {
            Id = "ALFKI",
            CompanyName = "Alfreds",
            ContactName = "Maria",
            City = "Berlin",
            Country = "Germany"
        });
    }

    [Fact]
    public async Task Search_ShouldTrimQuery_BeforeCallingRepo()
    {
        _repo.Setup(r => r.SearchByNameAsync("Alf", 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Customer>());

        var result = await _sut.Search("  Alf  ", CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>();
        _repo.Verify(r => r.SearchByNameAsync("Alf", 10, It.IsAny<CancellationToken>()), Times.Once);
        _repo.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Search_WithEmptyOrWhitespaceQuery_ShouldReturnEmpty_AndNotCallRepo(string q)
    {
        var result = await _sut.Search(q, CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.Should().BeEmpty();
        _repo.Verify(r => r.SearchByNameAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _repo.VerifyNoOtherCalls();
    }

    private static Customer CreateCustomer(
        string id,
        string companyName,
        string? contactName = null,
        string? city = null,
        string? region = null,
        string? country = null,
        string? phone = null,
        string? contactTitle = null)
        => new(
            id: id,
            companyName: companyName,
            contactName: contactName,
            contactTitle: contactTitle,
            city: city,
            region: region,
            country: country,
            phone: phone);
}