using AwesomeAssertions;
using Northwind.Application.Orders.Commands;
using Northwind.Application.Orders.Validators;
using Xunit;

namespace Northwind.Application.Tests.Orders;

public class ValidatorTests
{
    // ==================================================================
    // ShipOrderCommandValidator
    // ==================================================================

    [Fact]
    public void ShipOrderCommandValidator_WithValidCommand_ShouldPass()
    {
        var validator = new ShipOrderCommandValidator();
        var cmd = new ShipOrderCommand(OrderId: 1, ShipperId: 2);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ShipOrderCommandValidator_WithZeroOrderId_ShouldFail()
    {
        var validator = new ShipOrderCommandValidator();
        var cmd = new ShipOrderCommand(OrderId: 0, ShipperId: 1);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public void ShipOrderCommandValidator_WithZeroShipperId_ShouldFail()
    {
        var validator = new ShipOrderCommandValidator();
        var cmd = new ShipOrderCommand(OrderId: 1, ShipperId: 0);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShipperId");
    }

    [Fact]
    public void ShipOrderCommandValidator_WithFutureShippedDate_ShouldFail()
    {
        var validator = new ShipOrderCommandValidator();
        var cmd = new ShipOrderCommand(
            OrderId: 1,
            ShipperId: 2,
            ShippedDate: DateTime.UtcNow.AddDays(5));

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ShippedDate");
    }

    [Fact]
    public void ShipOrderCommandValidator_WithTodayAsShippedDate_ShouldPass()
    {
        var validator = new ShipOrderCommandValidator();
        var cmd = new ShipOrderCommand(
            OrderId: 1,
            ShipperId: 2,
            ShippedDate: DateTime.UtcNow);

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    // ==================================================================
    // UpdateOrderCommandValidator
    // ==================================================================

    [Fact]
    public void UpdateOrderCommandValidator_WithValidCommand_ShouldPass()
    {
        var validator = new UpdateOrderCommandValidator();
        var cmd = new UpdateOrderCommand(
            OrderId: 1, CustomerId: "ALFKI", EmployeeId: 1, ShipperId: null,
            ShipName: "John", ShipStreet: "123 St", ShipCity: "NYC",
            ShipRegion: null, ShipPostalCode: null, ShipCountry: "USA",
            Freight: 0, Lines: new List<OrderLineCommand> { new(1, 1, 0f) });

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateOrderCommandValidator_WithZeroOrderId_ShouldFail()
    {
        var validator = new UpdateOrderCommandValidator();
        var cmd = new UpdateOrderCommand(
            OrderId: 0, CustomerId: "ALFKI", EmployeeId: 1, ShipperId: null,
            ShipName: "John", ShipStreet: "123 St", ShipCity: "NYC",
            ShipRegion: null, ShipPostalCode: null, ShipCountry: "USA",
            Freight: 0, Lines: new List<OrderLineCommand> { new(1, 1, 0f) });

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "OrderId");
    }

    [Fact]
    public void UpdateOrderCommandValidator_WithEmptyLines_ShouldFail()
    {
        var validator = new UpdateOrderCommandValidator();
        var cmd = new UpdateOrderCommand(
            OrderId: 1, CustomerId: "ALFKI", EmployeeId: 1, ShipperId: null,
            ShipName: "John", ShipStreet: "123 St", ShipCity: "NYC",
            ShipRegion: null, ShipPostalCode: null, ShipCountry: "USA",
            Freight: 0, Lines: new List<OrderLineCommand>());

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Lines");
    }

    [Fact]
    public void UpdateOrderCommandValidator_WithNegativeFreight_ShouldFail()
    {
        var validator = new UpdateOrderCommandValidator();
        var cmd = new UpdateOrderCommand(
            OrderId: 1, CustomerId: "ALFKI", EmployeeId: 1, ShipperId: null,
            ShipName: "John", ShipStreet: "123 St", ShipCity: "NYC",
            ShipRegion: null, ShipPostalCode: null, ShipCountry: "USA",
            Freight: -5m, Lines: new List<OrderLineCommand> { new(1, 1, 0f) });

        var result = validator.Validate(cmd);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Freight");
    }
}