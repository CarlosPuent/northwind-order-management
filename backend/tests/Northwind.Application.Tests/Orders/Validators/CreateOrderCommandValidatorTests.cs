using AwesomeAssertions;
using Northwind.Application.Orders.Commands;
using Northwind.Application.Orders.Validators;
using Xunit;

namespace Northwind.Application.Tests.Orders.Validators;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    private static CreateOrderCommand ValidCommand() => new(
        CustomerId: "ALFKI",
        EmployeeId: 1,
        OrderDate: DateTime.UtcNow,
        ShipName: "John Doe",
        ShipStreet: "123 Main St",
        ShipCity: "NYC",
        ShipRegion: "NY",
        ShipPostalCode: "10001",
        ShipCountry: "USA",
        Freight: 15m,
        Lines: new List<OrderLineCommand> { new(11, 2, 0f) }
    );

    [Fact]
    public void ValidCommand_ShouldPass()
    {
        var result = _validator.Validate(ValidCommand());
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyCustomerId_ShouldFail(string? customerId)
    {
        var cmd = ValidCommand() with { CustomerId = customerId! };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ZeroEmployeeId_ShouldFail()
    {
        var cmd = ValidCommand() with { EmployeeId = 0 };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyShipName_ShouldFail()
    {
        var cmd = ValidCommand() with { ShipName = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyShipStreet_ShouldFail()
    {
        var cmd = ValidCommand() with { ShipStreet = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyShipCity_ShouldFail()
    {
        var cmd = ValidCommand() with { ShipCity = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyShipCountry_ShouldFail()
    {
        var cmd = ValidCommand() with { ShipCountry = "" };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void NegativeFreight_ShouldFail()
    {
        var cmd = ValidCommand() with { Freight = -1m };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void EmptyLines_ShouldFail()
    {
        var cmd = ValidCommand() with { Lines = new List<OrderLineCommand>() };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LineWithZeroQuantity_ShouldFail()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<OrderLineCommand> { new(11, 0, 0f) }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LineWithDiscountOver1_ShouldFail()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<OrderLineCommand> { new(11, 1, 1.5f) }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LineWithNegativeDiscount_ShouldFail()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<OrderLineCommand> { new(11, 1, -0.1f) }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void LineWithZeroProductId_ShouldFail()
    {
        var cmd = ValidCommand() with
        {
            Lines = new List<OrderLineCommand> { new(0, 1, 0f) }
        };
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
    }
}