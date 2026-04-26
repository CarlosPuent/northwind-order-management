using AwesomeAssertions;
using Northwind.Domain.Entities;
using Northwind.Domain.ValueObjects;
using Xunit;

namespace Northwind.Application.Tests.Entities;

/// <summary>
/// Tests for the Order aggregate root. These are not just unit tests — they are
/// the executable specification of the order lifecycle and its business rules.
/// Every invariant that <see cref="Order"/> enforces in code is verified here,
/// so regressions surface immediately.
/// </summary>
public class OrderTests
{
    // ----------------------------------------------------------------------
    // Helpers — keep tests focused on the rule under test, not on plumbing.
    // ----------------------------------------------------------------------

    private static Address SampleAddress() =>
        new("Calle Mayor 1", "Madrid", null, "28001", "Spain");

    private static Money Usd(decimal amount) => new(amount, "USD");

    private static Order CreateValidOrder()
    {
        var result = Order.Create(
            customerId: "ALFKI",
            employeeId: 1,
            orderDate: DateTime.UtcNow,
            shipName: "John Doe",
            shipAddress: SampleAddress(),
            freight: Usd(15m));

        // Tests use this helper; if Create ever fails the helper itself is broken.
        return result.Value;
    }

    // ----------------------------------------------------------------------
    // Construction
    // ----------------------------------------------------------------------

    [Fact]
    public void Create_WithValidInputs_ShouldSucceed()
    {
        var result = Order.Create(
            "ALFKI", 1, DateTime.UtcNow, "John Doe", SampleAddress(), Usd(10m));

        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be("ALFKI");
        result.Value.EmployeeId.Should().Be(1);
        result.Value.IsShipped.Should().BeFalse();
        result.Value.IsEditable.Should().BeTrue();
        result.Value.Lines.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithMissingCustomer_ShouldFail(string? customerId)
    {
        var result = Order.Create(
            customerId!, 1, DateTime.UtcNow, "John Doe", SampleAddress(), Usd(10m));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.MissingCustomer");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithInvalidEmployee_ShouldFail(int employeeId)
    {
        var result = Order.Create(
            "ALFKI", employeeId, DateTime.UtcNow, "John Doe", SampleAddress(), Usd(10m));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.MissingEmployee");
    }

    [Fact]
    public void Create_WithEmptyShipName_ShouldFail()
    {
        var result = Order.Create(
            "ALFKI", 1, DateTime.UtcNow, "", SampleAddress(), Usd(10m));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.MissingShipName");
    }

    [Fact]
    public void Create_WithFutureDate_ShouldFail()
    {
        var farFuture = DateTime.UtcNow.AddDays(30);

        var result = Order.Create(
            "ALFKI", 1, farFuture, "John Doe", SampleAddress(), Usd(10m));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.FutureDate");
    }

    // ----------------------------------------------------------------------
    // Adding lines
    // ----------------------------------------------------------------------

    [Fact]
    public void AddLine_WithValidInputs_ShouldAppendLine()
    {
        var order = CreateValidOrder();

        var result = order.AddLine(productId: 11, unitPrice: Usd(20m), quantity: 3, discount: 0f);

        result.IsSuccess.Should().BeTrue();
        order.Lines.Should().HaveCount(1);
        order.Lines.First().ProductId.Should().Be(11);
        order.Lines.First().Quantity.Should().Be(3);
    }

    [Fact]
    public void AddLine_WithSameProductTwice_ShouldMergeQuantities()
    {
        // Northwind's composite PK (OrderId, ProductId) doesn't allow duplicates.
        // Our domain makes the merge explicit so the rule is visible at the call site.
        var order = CreateValidOrder();

        order.AddLine(productId: 11, unitPrice: Usd(20m), quantity: 2, discount: 0f);
        order.AddLine(productId: 11, unitPrice: Usd(20m), quantity: 5, discount: 0f);

        order.Lines.Should().HaveCount(1);
        order.Lines.First().Quantity.Should().Be(7);
    }

    [Theory]
    [InlineData((short)0)]
    [InlineData((short)-1)]
    public void AddLine_WithNonPositiveQuantity_ShouldFail(short quantity)
    {
        var order = CreateValidOrder();

        var result = order.AddLine(productId: 11, unitPrice: Usd(20m), quantity: quantity, discount: 0f);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderLine.InvalidQuantity");
        order.Lines.Should().BeEmpty();
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void AddLine_WithDiscountOutOfRange_ShouldFail(float discount)
    {
        var order = CreateValidOrder();

        var result = order.AddLine(productId: 11, unitPrice: Usd(20m), quantity: 1, discount: discount);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderLine.InvalidDiscount");
    }

    [Fact]
    public void AddLine_WithDifferentCurrency_ShouldFail()
    {
        // A single order is always in a single currency. Mixing them would
        // mean the SubTotal calculation could fail at runtime — we prevent
        // the bad state at the entry point instead.
        var order = CreateValidOrder();
        order.AddLine(productId: 11, unitPrice: Usd(20m), quantity: 1, discount: 0f);

        var result = order.AddLine(
            productId: 12,
            unitPrice: new Money(20m, "EUR"),
            quantity: 1,
            discount: 0f);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("OrderLine.CurrencyMismatch");
    }

    // ----------------------------------------------------------------------
    // Removing lines
    // ----------------------------------------------------------------------

    [Fact]
    public void RemoveLine_WithExistingProduct_ShouldRemoveIt()
    {
        var order = CreateValidOrder();
        order.AddLine(11, Usd(20m), 1, 0f);
        order.AddLine(12, Usd(30m), 1, 0f);

        order.RemoveLine(11);

        order.Lines.Should().HaveCount(1);
        order.Lines.First().ProductId.Should().Be(12);
    }

    [Fact]
    public void RemoveLine_WithNonExistentProduct_ShouldBeNoOp()
    {
        // Removing something that isn't there is not an error — it's a no-op.
        // This makes the operation idempotent, which matters when a user
        // double-clicks a "remove" button or a request retries.
        var order = CreateValidOrder();
        order.AddLine(11, Usd(20m), 1, 0f);

        var result = order.RemoveLine(999);

        result.IsSuccess.Should().BeTrue();
        order.Lines.Should().HaveCount(1);
    }

    // ----------------------------------------------------------------------
    // Lines collection encapsulation
    // ----------------------------------------------------------------------

    [Fact]
    public void Lines_ShouldBeReadOnly_NotAllowingExternalMutation()
    {
        // The whole point of the aggregate root pattern: nobody can bypass
        // AddLine/RemoveLine and mutate the internal collection directly.
        var order = CreateValidOrder();

        order.Lines.Should().BeAssignableTo<IReadOnlyCollection<OrderLine>>();
    }

    // ----------------------------------------------------------------------
    // Totals (computed properties)
    // ----------------------------------------------------------------------

    [Fact]
    public void SubTotal_WithNoLines_ShouldBeZero()
    {
        var order = CreateValidOrder();

        order.SubTotal.Amount.Should().Be(0m);
    }

    [Fact]
    public void SubTotal_WithMultipleLines_ShouldBeSumOfLineTotals()
    {
        var order = CreateValidOrder();
        order.AddLine(11, Usd(10m), 2, 0f);    // 20
        order.AddLine(12, Usd(15m), 4, 0f);    // 60

        order.SubTotal.Amount.Should().Be(80m);
    }

    [Fact]
    public void SubTotal_WithDiscount_ShouldApplyDiscount()
    {
        var order = CreateValidOrder();
        order.AddLine(11, Usd(100m), 1, 0.10f);  // 100 * 0.90 = 90

        order.SubTotal.Amount.Should().Be(90m);
    }

    [Fact]
    public void Total_ShouldEqualSubTotalPlusFreight()
    {
        var order = CreateValidOrder();          // freight = 15
        order.AddLine(11, Usd(100m), 1, 0f);     // subtotal = 100

        order.Total.Amount.Should().Be(115m);
    }

    // ----------------------------------------------------------------------
    // Shipper assignment
    // ----------------------------------------------------------------------

    [Fact]
    public void AssignShipper_WithValidId_ShouldSucceed()
    {
        var order = CreateValidOrder();

        var result = order.AssignShipper(2);

        result.IsSuccess.Should().BeTrue();
        order.ShipperId.Should().Be(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AssignShipper_WithInvalidId_ShouldFail(int shipperId)
    {
        var order = CreateValidOrder();

        var result = order.AssignShipper(shipperId);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.InvalidShipper");
    }

    // ----------------------------------------------------------------------
    // The lifecycle: shipping freezes the order
    // ----------------------------------------------------------------------

    [Fact]
    public void MarkAsShipped_WithLinesAndShipper_ShouldSucceed()
    {
        var order = CreateValidOrder();
        order.AddLine(11, Usd(10m), 1, 0f);
        order.AssignShipper(2);

        var result = order.MarkAsShipped(DateTime.UtcNow);

        result.IsSuccess.Should().BeTrue();
        order.IsShipped.Should().BeTrue();
        order.IsEditable.Should().BeFalse();
    }

    [Fact]
    public void MarkAsShipped_WithoutLines_ShouldFail()
    {
        var order = CreateValidOrder();
        order.AssignShipper(2);

        var result = order.MarkAsShipped(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.EmptyOrder");
    }

    [Fact]
    public void MarkAsShipped_WithoutShipper_ShouldFail()
    {
        var order = CreateValidOrder();
        order.AddLine(11, Usd(10m), 1, 0f);

        var result = order.MarkAsShipped(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NoShipper");
    }

    [Fact]
    public void MarkAsShipped_Twice_ShouldFailOnSecondAttempt()
    {
        var order = CreateValidOrder();
        order.AddLine(11, Usd(10m), 1, 0f);
        order.AssignShipper(2);
        order.MarkAsShipped(DateTime.UtcNow);

        var result = order.MarkAsShipped(DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.AlreadyShipped");
    }

    [Fact]
    public void AddLine_AfterShipped_ShouldFail()
    {
        // The most important invariant in the lifecycle: shipping freezes the order.
        var order = CreateValidOrder();
        order.AddLine(11, Usd(10m), 1, 0f);
        order.AssignShipper(2);
        order.MarkAsShipped(DateTime.UtcNow);

        var result = order.AddLine(12, Usd(20m), 1, 0f);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NotEditable");
    }

    [Fact]
    public void RemoveLine_AfterShipped_ShouldFail()
    {
        var order = CreateValidOrder();
        order.AddLine(11, Usd(10m), 1, 0f);
        order.AssignShipper(2);
        order.MarkAsShipped(DateTime.UtcNow);

        var result = order.RemoveLine(11);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NotEditable");
    }

    [Fact]
    public void UpdateShipAddress_AfterShipped_ShouldFail()
    {
        var order = CreateValidOrder();
        order.AddLine(11, Usd(10m), 1, 0f);
        order.AssignShipper(2);
        order.MarkAsShipped(DateTime.UtcNow);

        var result = order.UpdateShipAddress(SampleAddress());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Order.NotEditable");
    }
}