using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Northwind.Api.Controllers;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Domain.Entities;
using Xunit;

namespace Northwind.Api.Tests.Controllers;

public sealed class EmployeesControllerTests
{
    private readonly Mock<IEmployeeRepository> _repo = new();
    private readonly EmployeesController _sut;

    public EmployeesControllerTests()
    {
        _sut = new EmployeesController(_repo.Object);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOk_WithProjectedEmployees()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>
            {
                new(id: 1, firstName: "Nancy",  lastName: "Davolio", title: "Sales Rep", city: "Seattle",  country: "USA"),
                new(id: 2, firstName: "Andrew", lastName: "Fuller",  title: "VP Sales",  city: "Tacoma",   country: "USA")
            });

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var items = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.ToList();
        items.Should().HaveCount(2);

        items[0].Should().BeEquivalentTo(new
        {
            Id = 1,
            FirstName = "Nancy",
            LastName = "Davolio",
            FullName = "Nancy Davolio",
            Title = "Sales Rep",
            City = "Seattle",
            Country = "USA"
        });
    }

    [Fact]
    public async Task GetAll_WhenNoEmployees_ShouldReturnOkWithEmptyList()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Employee>());

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAll_ShouldConcatenateFirstAndLastName_InFullName()
    {
        _repo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Employee>
            {
                new(id: 3, firstName: "Janet", lastName: "Leverling",
                    title: "Sales Rep", city: "Kirkland", country: "USA")
            });

        var result = await _sut.GetAll(CancellationToken.None);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var item = ok.Value.Should().BeAssignableTo<IEnumerable<object>>().Subject.Single();

        item.Should().BeEquivalentTo(new
        {
            Id = 3,
            FirstName = "Janet",
            LastName = "Leverling",
            FullName = "Janet Leverling",
            Title = "Sales Rep",
            City = "Kirkland",
            Country = "USA"
        });
    }
}
