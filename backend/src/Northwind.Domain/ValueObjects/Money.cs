using Northwind.Domain.Common;

namespace Northwind.Domain.ValueObjects;

/// <summary>
/// A monetary amount in a specific currency.
/// Value object — two Money instances with the same amount and currency are equal,
/// regardless of object identity.
/// </summary>
/// <remarks>
/// Modeled as a value object (record) so equality, hashing, and immutability are
/// handled by the language. Use <see cref="Create"/> for safe construction with
/// validation; the public constructor exists only because EF Core needs it.
/// </remarks>
public sealed record Money
{
    /// <summary>The numeric amount. Always non-negative — see <see cref="Create"/>.</summary>
    public decimal Amount { get; init; }

    /// <summary>ISO 4217 currency code, e.g. "USD", "EUR".</summary>
    public string Currency { get; init; }

    /// <summary>A zero amount in USD — useful as a default for empty orders.</summary>
    public static Money Zero => new(0m, "USD");

    // Public-but-discouraged constructor required by EF Core for materialization.
    // Production code should use Create(...) so validation runs.
    public Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Safely constructs a Money. Returns a failure Result if the amount is negative
    /// or the currency code is missing/invalid.
    /// </summary>
    public static Result<Money> Create(decimal amount, string currency)
    {
        if (amount < 0)
            return Error.Validation(
                "Money.NegativeAmount",
                "Monetary amounts cannot be negative.");

        if (string.IsNullOrWhiteSpace(currency))
            return Error.Validation(
                "Money.MissingCurrency",
                "Currency code is required.");

        if (currency.Length != 3)
            return Error.Validation(
                "Money.InvalidCurrency",
                "Currency code must be a 3-letter ISO 4217 code (e.g. 'USD').");

        return new Money(amount, currency.ToUpperInvariant());
    }

    /// <summary>
    /// Adds another Money of the same currency. Returns a failure if the currencies
    /// differ — adding USD to EUR is a domain bug, not a number problem.
    /// </summary>
    public Result<Money> Add(Money other)
    {
        if (other.Currency != Currency)
            return Error.Validation(
                "Money.CurrencyMismatch",
                $"Cannot add {Currency} to {other.Currency}.");

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Multiplies the amount by a factor (e.g. quantity × unit price).
    /// </summary>
    public Money Multiply(int factor) => new(Amount * factor, Currency);

    public override string ToString() => $"{Amount:F2} {Currency}";
}