using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Northwind.Api.Controllers;
using Northwind.Application.Abstractions;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Domain.Common;
using Northwind.Domain.Entities;
using Northwind.Domain.ValueObjects;
using Xunit;

namespace Northwind.Api.Tests.Controllers;

public sealed class InvoicesControllerTests
{
    private readonly Mock<IOrderRepository> _orders = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<IEmployeeRepository> _employees = new();
    private readonly Mock<IShipperRepository> _shippers = new();
    private readonly Mock<IShippingGeocodeRepository> _geocodes = new();
    private readonly Mock<IProductRepository> _products = new();
    private readonly Mock<IInvoiceGenerator> _generator = new();
    private readonly InvoicesController _sut;

    public InvoicesControllerTests()
    {
        _sut = new InvoicesController(
            _orders.Object, _customers.Object, _employees.Object,
            _shippers.Object, _geocodes.Object, _products.Object,
            _generator.Object);
    }

    [Fact]
    public async Task Generate_WhenOrderNotFound_ShouldReturn404()
    {
        _orders.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _sut.Generate(999, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task Generate_WhenInvoiceGenerationFails_ShouldReturn500()
    {
        var orderId = 10248;
        var order = BuildOrder(customerId: "ALFKI", employeeId: 1);
        SetupHappyPath(orderId, order);

        _generator
            .Setup(g => g.GenerateAsync(
                order, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<ShippingGeocode?>(),
                It.IsAny<Dictionary<int, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Error.Failure("Pdf.Error", "PDF generation failed."));

        var result = await _sut.Generate(orderId, CancellationToken.None);

        result.Should().BeOfType<ObjectResult>()
            .Which.StatusCode.Should().Be(500);
    }

    [Fact]
    public async Task Generate_WhenSuccessful_ShouldReturnPdfFile()
    {
        var orderId = 10248;
        var order = BuildOrder(customerId: "ALFKI", employeeId: 1);
        var pdfBytes = new byte[] { 1, 2, 3 };
        SetupHappyPath(orderId, order);

        _generator
            .Setup(g => g.GenerateAsync(
                order, It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string?>(), It.IsAny<ShippingGeocode?>(),
                It.IsAny<Dictionary<int, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pdfBytes);

        var result = await _sut.Generate(orderId, CancellationToken.None);

        var file = result.Should().BeOfType<FileContentResult>().Subject;
        file.ContentType.Should().Be("application/pdf");
        file.FileContents.Should().BeEquivalentTo(pdfBytes);
    }

    [Fact]
    public async Task Generate_OrderWithShipper_ShouldFetchShipperName()
    {
        var orderId = 10248;
        var order = BuildOrder(customerId: "ALFKI", employeeId: 1, shipperId: 2);
        var pdfBytes = new byte[] { 1 };
        SetupHappyPath(orderId, order);

        _shippers.Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Shipper(id: 2, companyName: "United Package", phone: "555-1234"));

        _generator
            .Setup(g => g.GenerateAsync(
                order, It.IsAny<string>(), It.IsAny<string>(),
                "United Package", It.IsAny<ShippingGeocode?>(),
                It.IsAny<Dictionary<int, string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pdfBytes);

        var result = await _sut.Generate(orderId, CancellationToken.None);

        result.Should().BeOfType<FileContentResult>();
    }

    // ---- helpers ----

    private static Order BuildOrder(string customerId, int employeeId, int? shipperId = null)
    {
        var address = Address.Create(
            street: "123 St",
            city: "Berlin",
            region: null,
            postalCode: null,
            country: "Germany").Value;

        var order = Order.Create(
            customerId: customerId,
            employeeId: employeeId,
            orderDate: DateTime.UtcNow,
            shipName: "Test Ship",
            shipAddress: address,
            freight: new Money(10m, "USD")).Value;

        if (shipperId.HasValue)
            order.AssignShipper(shipperId.Value);

        return order;
    }

    private void SetupHappyPath(int orderId, Order order)
    {
        _orders.Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _customers.Setup(r => r.GetByIdAsync(order.CustomerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Customer(
                id: order.CustomerId,
                companyName: "Alfreds",
                contactName: null,
                contactTitle: null,
                city: null,
                region: null,
                country: null,
                phone: null));

        _employees.Setup(r => r.GetByIdAsync(order.EmployeeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Employee(
                id: order.EmployeeId,
                firstName: "Nancy",
                lastName: "Davolio",
                title: null,
                city: null,
                country: null));

        _geocodes.Setup(r => r.GetByOrderIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ShippingGeocode?)null);
    }
}
