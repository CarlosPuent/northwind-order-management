using AwesomeAssertions;
using Northwind.Domain.ValueObjects;
using Xunit;

namespace Northwind.Application.Tests.ValueObjects;

/// <summary>
/// Tests for the Money value object. Covers safe construction, arithmetic
/// operations, and the invariants we promised in the type's contract.
/// </summary>
public class MoneyTests
{
    // ---- Construction ----

    [Fact]
    public void Create_WithValidAmountAndCurrency_ShouldSucceed()
    {
        var result = Money.Create(100m, "USD");

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(100m);
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Create_WithLowercaseCurrency_ShouldNormalizeToUppercase()
    {
        var result = Money.Create(50m, "eur");

        result.IsSuccess.Should().BeTrue();
        result.Value.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Create_WithNegativeAmount_ShouldFail()
    {
        var result = Money.Create(-1m, "USD");

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.NegativeAmount");
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData(null)]
    public void Create_WithMissingCurrency_ShouldFail(string? currency)
    {
        var result = Money.Create(100m, currency!);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.MissingCurrency");
    }

    [Theory]
    [InlineData("US")]      // too short
    [InlineData("USDD")]    // too long
    public void Create_WithInvalidCurrencyLength_ShouldFail(string currency)
    {
        var result = Money.Create(100m, currency);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.InvalidCurrency");
    }

    // ---- Equality (the value-object contract) ----

    [Fact]
    public void TwoMoneys_WithSameAmountAndCurrency_ShouldBeEqual()
    {
        // The whole point of being a value object: identity doesn't matter,
        // only the values do.
        var a = new Money(100m, "USD");
        var b = new Money(100m, "USD");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    // ---- Arithmetic ----

    [Fact]
    public void Add_WithSameCurrency_ShouldReturnSummedMoney()
    {
        var a = new Money(100m, "USD");
        var b = new Money(50m, "USD");

        var result = a.Add(b);

        result.IsSuccess.Should().BeTrue();
        result.Value.Amount.Should().Be(150m);
        result.Value.Currency.Should().Be("USD");
    }

    [Fact]
    public void Add_WithDifferentCurrencies_ShouldFail()
    {
        // Adding USD + EUR is meaningless — we want a domain error, not a silent bug.
        var usd = new Money(100m, "USD");
        var eur = new Money(50m, "EUR");

        var result = usd.Add(eur);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Money.CurrencyMismatch");
    }

    [Fact]
    public void Multiply_ShouldScaleAmountKeepingCurrency()
    {
        var price = new Money(25m, "USD");

        var total = price.Multiply(4);

        total.Amount.Should().Be(100m);
        total.Currency.Should().Be("USD");
    }

    [Fact]
    public void Zero_ShouldBeUsdWithAmountZero()
    {
        Money.Zero.Amount.Should().Be(0m);
        Money.Zero.Currency.Should().Be("USD");
    }
}