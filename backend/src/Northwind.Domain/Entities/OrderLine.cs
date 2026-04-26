using Northwind.Domain.Common;
using Northwind.Domain.ValueObjects;

namespace Northwind.Domain.Entities;

/// <summary>
/// A single line item within an Order: which product, how many, at what unit price,
/// with what discount. OrderLine is part of the Order aggregate — it is never
/// constructed or modified outside of Order's methods.
/// </summary>
/// <remarks>
/// Northwind's Order Details table uses a composite key (OrderId, ProductId).
/// We model each line as having its own surrogate Id internally for simpler
/// aggregate management, and configure EF Core to map to the composite key.
/// </remarks>
public sealed class OrderLine : Entity<int>
{
    public int OrderId { get; private set; }
    public int ProductId { get; private set; }
    public Money UnitPrice { get; private set; }
    public short Quantity { get; private set; }

    /// <summary>Discount as a decimal between 0 and 1 (e.g. 0.15 = 15% off).</summary>
    public float Discount { get; private set; }

    /// <summary>
    /// The total for this line: UnitPrice × Quantity × (1 - Discount).
    /// Computed on the fly so it can never get out of sync with its inputs.
    /// </summary>
    public Money LineTotal
    {
        get
        {
            var gross = UnitPrice.Multiply(Quantity);
            var discountFactor = (decimal)(1f - Discount);
            return new Money(gross.Amount * discountFactor, gross.Currency);
        }
    }

    // Internal constructor — only Order can create lines.
    // This is the aggregate-root pattern: external code cannot bypass Order's rules.
    internal OrderLine(int productId, Money unitPrice, short quantity, float discount)
    {
        ProductId = productId;
        UnitPrice = unitPrice;
        Quantity = quantity;
        Discount = discount;
    }

    // Parameterless constructor for EF Core only.
    private OrderLine() : base()
    {
        UnitPrice = Money.Zero;
    }

    /// <summary>
    /// Updates the quantity. Internal — Order coordinates these changes
    /// and is the only one that should call this method.
    /// </summary>
    internal void UpdateQuantity(short newQuantity)
    {
        Quantity = newQuantity;
    }
}