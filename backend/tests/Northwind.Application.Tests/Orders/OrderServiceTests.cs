using AwesomeAssertions;
using Moq;
using Northwind.Application.Abstractions.Persistence;
using Northwind.Application.Orders;
using Northwind.Application.Orders.Commands;
using Northwind.Domain.Entities;
using Northwind.Domain.ValueObjects;
using Xunit;

namespace Northwind.Application.Tests.Orders;

/// <summary>
/// Tests for OrderService — the core application service that orchestrates
/// order creation, updates, and deletion. These tests prove that business
/// logic works correctly without any database or HTTP dependency.
/// </summary>
public class OrderServiceTests
{
    // ---- Shared mocks ----

    private readonly Mock<IOrderRepository> _orderRepo = new();
    private readonly Mock<IProductRepository> _productRepo = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly OrderService _sut;

    public OrderServiceTests()
    {
        _unitOfWork.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _sut = new OrderService(_orderRepo.Object, _productRepo.Object, _unitOfWork.Object);
    }

    // ---- Helpers ----

    private static Product FakeProduct(int id, decimal price) =>
        new(id, $"Product {id}", new Money(price, "USD"), false, 100);

    private static CreateOrderCommand ValidCreateCommand(List<OrderLineCommand>? lines = null) =>
        new(
            CustomerId: "ALFKI",
            EmployeeId: 1,
            OrderDate: DateTime.UtcNow,
            ShipName: "John Doe",
            ShipStreet: "123 Main St",
            ShipCity: "New York",
            ShipRegion: "NY",
            ShipPostalCode: "10001",
            ShipCountry: "USA",
            Freight: 15m,
            Lines: lines ?? new List<OrderLineCommand>
            {
                new(ProductId: 11, Quantity: 2, Discount: 0f)
            }
        );

    // ==================================================================
    // CreateAsync
    // ==================================================================

    [Fact]
    public async Task CreateAsync_WithValidCommand_ShouldReturnSuccess()
    {
        _productRepo.Setup(p => p.GetByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeProduct(11, 20m));

        var result = await _sut.CreateAsync(ValidCreateCommand());

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be("ALFKI");
        result.Value.Lines.Should().HaveCount(1);
        _orderRepo.Verify(r => r.Add(It.IsAny<Order>()), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithNoLines_ShouldReturnFailure()
    {
        var cmd = ValidCreateCommand(lines: new List<OrderLineCommand>());

        var result = await _sut.CreateAsync(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NoLines");
        _orderRepo.Verify(r => r.Add(It.IsAny<Order>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_WithMissingCustomer_ShouldReturnFailure()
    {
        var cmd = new CreateOrderCommand(
            CustomerId: "",
            EmployeeId: 1,
            OrderDate: DateTime.UtcNow,
            ShipName: "John",
            ShipStreet: "123 St",
            ShipCity: "NYC",
            ShipRegion: null,
            ShipPostalCode: null,
            ShipCountry: "USA",
            Freight: 0,
            Lines: new List<OrderLineCommand> { new(11, 1, 0f) }
        );

        var result = await _sut.CreateAsync(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.MissingCustomer");
    }

    [Fact]
    public async Task CreateAsync_WithMissingEmployee_ShouldReturnFailure()
    {
        var cmd = new CreateOrderCommand(
            CustomerId: "ALFKI",
            EmployeeId: 0,
            OrderDate: DateTime.UtcNow,
            ShipName: "John",
            ShipStreet: "123 St",
            ShipCity: "NYC",
            ShipRegion: null,
            ShipPostalCode: null,
            ShipCountry: "USA",
            Freight: 0,
            Lines: new List<OrderLineCommand> { new(11, 1, 0f) }
        );

        var result = await _sut.CreateAsync(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.MissingEmployee");
    }

    [Fact]
    public async Task CreateAsync_WithMissingShipName_ShouldReturnFailure()
    {
        var cmd = new CreateOrderCommand(
            CustomerId: "ALFKI",
            EmployeeId: 1,
            OrderDate: DateTime.UtcNow,
            ShipName: "",
            ShipStreet: "123 St",
            ShipCity: "NYC",
            ShipRegion: null,
            ShipPostalCode: null,
            ShipCountry: "USA",
            Freight: 0,
            Lines: new List<OrderLineCommand> { new(11, 1, 0f) }
        );

        var result = await _sut.CreateAsync(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.MissingShipName");
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentProduct_ShouldReturnFailure()
    {
        _productRepo.Setup(p => p.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var cmd = ValidCreateCommand(lines: new List<OrderLineCommand>
        {
            new(ProductId: 999, Quantity: 1, Discount: 0f)
        });

        var result = await _sut.CreateAsync(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.ProductNotFound");
    }

    [Fact]
    public async Task CreateAsync_WithNegativeFreight_ShouldReturnFailure()
    {
        var cmd = new CreateOrderCommand(
            CustomerId: "ALFKI",
            EmployeeId: 1,
            OrderDate: DateTime.UtcNow,
            ShipName: "John",
            ShipStreet: "123 St",
            ShipCity: "NYC",
            ShipRegion: null,
            ShipPostalCode: null,
            ShipCountry: "USA",
            Freight: -10m,
            Lines: new List<OrderLineCommand> { new(11, 1, 0f) }
        );

        var result = await _sut.CreateAsync(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.NegativeAmount");
    }

    [Fact]
    public async Task CreateAsync_WithMissingShipStreet_ShouldReturnFailure()
    {
        var cmd = new CreateOrderCommand(
            CustomerId: "ALFKI",
            EmployeeId: 1,
            OrderDate: DateTime.UtcNow,
            ShipName: "John",
            ShipStreet: "",
            ShipCity: "NYC",
            ShipRegion: null,
            ShipPostalCode: null,
            ShipCountry: "USA",
            Freight: 0,
            Lines: new List<OrderLineCommand> { new(11, 1, 0f) }
        );

        var result = await _sut.CreateAsync(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Address.MissingStreet");
    }

    // ==================================================================
    // GetByIdAsync
    // ==================================================================

    [Fact]
    public async Task GetByIdAsync_WithExistingOrder_ShouldReturnDto()
    {
        var order = CreateSampleOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _sut.GetByIdAsync(1);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(0); // New order, not yet persisted
        result.Value.CustomerId.Should().Be("ALFKI");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentOrder_ShouldReturnFailure()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _sut.GetByIdAsync(999);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NotFound");
    }

    // ==================================================================
    // DeleteAsync
    // ==================================================================

    [Fact]
    public async Task DeleteAsync_WithExistingPendingOrder_ShouldSucceed()
    {
        var order = CreateSampleOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _sut.DeleteAsync(1);

        result.IsSuccess.Should().BeTrue();
        _orderRepo.Verify(r => r.Remove(order), Times.Once);
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentOrder_ShouldReturnFailure()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var result = await _sut.DeleteAsync(999);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NotFound");
    }

    [Fact]
    public async Task DeleteAsync_WithShippedOrder_ShouldReturnFailure()
    {
        var order = CreateShippedOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var result = await _sut.DeleteAsync(1);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.AlreadyShipped");
        _orderRepo.Verify(r => r.Remove(It.IsAny<Order>()), Times.Never);
    }

    // ==================================================================
    // Helpers
    // ==================================================================

    private static Order CreateSampleOrder()
    {
        var address = new Address("123 Main St", "NYC", "NY", "10001", "USA");
        var result = Order.Create("ALFKI", 1, DateTime.UtcNow, "John Doe", address, new Money(15m, "USD"));
        var order = result.Value;
        order.AddLine(11, new Money(20m, "USD"), 2, 0f);
        return order;
    }

    private static Order CreateShippedOrder()
    {
        var order = CreateSampleOrder();
        order.AssignShipper(1);
        order.MarkAsShipped(DateTime.UtcNow);
        return order;
    }

    // ==================================================================
    // UpdateAsync
    // ==================================================================

    [Fact]
    public async Task UpdateAsync_WithValidCommand_ShouldReturnSuccess()
    {
        var order = CreateSampleOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);
        _productRepo.Setup(p => p.GetByIdAsync(11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(FakeProduct(11, 20m));

        var cmd = new UpdateOrderCommand(
            OrderId: 1,
            CustomerId: "ALFKI",
            EmployeeId: 1,
            ShipperId: null,
            ShipName: "Jane Doe",
            ShipStreet: "456 Oak Ave",
            ShipCity: "LA",
            ShipRegion: "CA",
            ShipPostalCode: "90001",
            ShipCountry: "USA",
            Freight: 20m,
            Lines: new List<OrderLineCommand> { new(11, 3, 0f) }
        );

        var result = await _sut.UpdateAsync(cmd);

        result.IsSuccess.Should().BeTrue();
        _unitOfWork.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentOrder_ShouldReturnFailure()
    {
        _orderRepo.Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var cmd = new UpdateOrderCommand(
            OrderId: 999, CustomerId: "ALFKI", EmployeeId: 1, ShipperId: null,
            ShipName: "John", ShipStreet: "123 St", ShipCity: "NYC",
            ShipRegion: null, ShipPostalCode: null, ShipCountry: "USA",
            Freight: 0, Lines: new List<OrderLineCommand> { new(11, 1, 0f) }
        );

        var result = await _sut.UpdateAsync(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NotFound");
    }

    [Fact]
    public async Task UpdateAsync_WithShippedOrder_ShouldReturnFailure()
    {
        var order = CreateShippedOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var cmd = new UpdateOrderCommand(
            OrderId: 1, CustomerId: "ALFKI", EmployeeId: 1, ShipperId: null,
            ShipName: "John", ShipStreet: "123 St", ShipCity: "NYC",
            ShipRegion: null, ShipPostalCode: null, ShipCountry: "USA",
            Freight: 0, Lines: new List<OrderLineCommand> { new(11, 1, 0f) }
        );

        var result = await _sut.UpdateAsync(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NotEditable");
    }

    [Fact]
    public async Task UpdateAsync_WithNoLines_ShouldReturnFailure()
    {
        var order = CreateSampleOrder();
        _orderRepo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var cmd = new UpdateOrderCommand(
            OrderId: 1, CustomerId: "ALFKI", EmployeeId: 1, ShipperId: null,
            ShipName: "John", ShipStreet: "123 St", ShipCity: "NYC",
            ShipRegion: null, ShipPostalCode: null, ShipCountry: "USA",
            Freight: 0, Lines: new List<OrderLineCommand>()
        );

        var result = await _sut.UpdateAsync(cmd);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NoLines");
    }

    // ==================================================================
    // GetPagedAsync
    // ==================================================================

    [Fact]
    public async Task GetPagedAsync_ShouldReturnPagedResult()
    {
        var orders = new List<Order> { CreateSampleOrder() };
        var paged = new Northwind.Application.Common.PagedResult<Order>(
            orders.AsReadOnly(), 1, 10, 1);

        _orderRepo.Setup(r => r.GetPagedAsync(1, 10, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(paged);

        var result = await _sut.GetPagedAsync(1, 10);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }
}