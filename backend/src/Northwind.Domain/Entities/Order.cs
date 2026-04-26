using Northwind.Domain.Common;
using Northwind.Domain.ValueObjects;

namespace Northwind.Domain.Entities;

/// <summary>
/// An order placed by a Customer, processed by an Employee, and shipped via a Shipper.
/// Order is the aggregate root — all modifications to its lines and shipping info
/// flow through methods on this class so invariants stay consistent.
/// </summary>
/// <remarks>
/// Status is derived from <see cref="ShippedDate"/> rather than stored as a separate
/// column, because the Northwind schema doesn't have a Status column and we honor
/// that constraint instead of altering the legacy table.
/// </remarks>
public sealed class Order : Entity<int>
{
    // ----- Properties (private setters; mutated via methods) -----

    public string CustomerId { get; private set; }
    public int EmployeeId { get; private set; }
    public int? ShipperId { get; private set; }
    public DateTime OrderDate { get; private set; }
    public DateTime? RequiredDate { get; private set; }
    public DateTime? ShippedDate { get; private set; }
    public Money Freight { get; private set; }
    public string ShipName { get; private set; }
    public Address ShipAddress { get; private set; }

    /// <summary>
    /// Order status derived from ShippedDate. Pending if not yet shipped, Shipped otherwise.
    /// Northwind's schema doesn't store a status column — the presence of a ShippedDate
    /// is the source of truth.
    /// </summary>
    public bool IsShipped => ShippedDate.HasValue;

    /// <summary>True while the order can still be modified. Once shipped, lines are frozen.</summary>
    public bool IsEditable => !IsShipped;

    // ----- Lines: encapsulated collection (the key DDD pattern) -----

    private readonly List<OrderLine> _lines = new();

    /// <summary>
    /// Read-only view of the order's lines. External code can read the lines
    /// but cannot add, remove or modify them — those operations go through
    /// AddLine and RemoveLine to keep invariants intact.
    /// </summary>
    public IReadOnlyCollection<OrderLine> Lines => _lines.AsReadOnly();

    /// <summary>The sum of all line totals. Computed on demand.</summary>
    public Money SubTotal
    {
        get
        {
            if (_lines.Count == 0) return Money.Zero;

            var sum = _lines[0].LineTotal;
            for (int i = 1; i < _lines.Count; i++)
            {
                // Currency consistency is enforced when lines are added,
                // so this Add cannot fail in practice.
                sum = sum.Add(_lines[i].LineTotal).Value;
            }
            return sum;
        }
    }

    /// <summary>The grand total: subtotal plus freight.</summary>
    public Money Total
    {
        get
        {
            var addResult = SubTotal.Add(Freight);
            return addResult.IsSuccess ? addResult.Value : SubTotal;
        }
    }

    // ----- Construction -----

    // Private constructor — use Order.Create(...) which validates inputs.
    private Order(
        string customerId,
        int employeeId,
        DateTime orderDate,
        string shipName,
        Address shipAddress,
        Money freight)
    {
        CustomerId = customerId;
        EmployeeId = employeeId;
        OrderDate = orderDate;
        ShipName = shipName;
        ShipAddress = shipAddress;
        Freight = freight;
    }

    // Parameterless constructor for EF Core only.
    private Order() : base()
    {
        CustomerId = string.Empty;
        ShipName = string.Empty;
        ShipAddress = null!;
        Freight = Money.Zero;
    }

    /// <summary>
    /// Factory method to create a new Order. Validates inputs and returns a
    /// failure Result if any are invalid. The resulting order has no lines yet —
    /// at least one line must be added (via <see cref="AddLine"/>) before persisting.
    /// </summary>
    public static Result<Order> Create(
        string customerId,
        int employeeId,
        DateTime orderDate,
        string shipName,
        Address shipAddress,
        Money freight)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            return Error.Validation("Order.MissingCustomer", "Customer is required.");

        if (employeeId <= 0)
            return Error.Validation("Order.MissingEmployee", "Employee is required.");

        if (string.IsNullOrWhiteSpace(shipName))
            return Error.Validation("Order.MissingShipName", "Recipient name is required.");

        if (orderDate > DateTime.UtcNow.AddDays(1))
            return Error.Validation("Order.FutureDate", "Order date cannot be in the future.");

        return new Order(customerId, employeeId, orderDate, shipName, shipAddress, freight);
    }

    // ----- Line management -----

    /// <summary>
    /// Adds a product line to the order. If the same product is already on the
    /// order, the existing line's quantity is increased instead of creating a duplicate.
    /// </summary>
    public Result AddLine(int productId, Money unitPrice, short quantity, float discount)
    {
        if (!IsEditable)
            return Error.Conflict(
                "Order.NotEditable",
                "Cannot modify lines on an order that has already shipped.");

        if (quantity <= 0)
            return Error.Validation("OrderLine.InvalidQuantity", "Quantity must be positive.");

        if (discount is < 0 or > 1)
            return Error.Validation(
                "OrderLine.InvalidDiscount",
                "Discount must be between 0 and 1.");

        if (_lines.Count > 0 && _lines[0].UnitPrice.Currency != unitPrice.Currency)
            return Error.Validation(
                "OrderLine.CurrencyMismatch",
                "All lines in an order must share the same currency.");

        // Merge duplicates: Northwind's composite key (OrderId, ProductId) wouldn't
        // allow two rows for the same product anyway. We make the merge explicit.
        var existing = _lines.FirstOrDefault(l => l.ProductId == productId);
        if (existing != null)
        {
            existing.UpdateQuantity((short)(existing.Quantity + quantity));
            return Result.Success();
        }

        _lines.Add(new OrderLine(productId, unitPrice, quantity, discount));
        return Result.Success();
    }

    /// <summary>
    /// Removes a product line from the order. No-op if the product isn't on the order.
    /// </summary>
    public Result RemoveLine(int productId)
    {
        if (!IsEditable)
            return Error.Conflict(
                "Order.NotEditable",
                "Cannot modify lines on an order that has already shipped.");

        var line = _lines.FirstOrDefault(l => l.ProductId == productId);
        if (line != null) _lines.Remove(line);

        return Result.Success();
    }

    // ----- Shipper / address / shipping date -----

    public Result AssignShipper(int shipperId)
    {
        if (!IsEditable)
            return Error.Conflict(
                "Order.NotEditable",
                "Cannot assign a shipper to an order that has already shipped.");

        if (shipperId <= 0)
            return Error.Validation("Order.InvalidShipper", "Shipper id must be positive.");

        ShipperId = shipperId;
        return Result.Success();
    }

    public Result UpdateShipAddress(Address newAddress)
    {
        if (!IsEditable)
            return Error.Conflict(
                "Order.NotEditable",
                "Cannot change the shipping address on an order that has already shipped.");

        ShipAddress = newAddress;
        return Result.Success();
    }

    public Result UpdateShipName(string newShipName)
    {
        if (!IsEditable)
            return Error.Conflict(
                "Order.NotEditable",
                "Cannot change the recipient on an order that has already shipped.");

        if (string.IsNullOrWhiteSpace(newShipName))
            return Error.Validation("Order.MissingShipName", "Recipient name is required.");

        ShipName = newShipName;
        return Result.Success();
    }

    public Result MarkAsShipped(DateTime shippedDate)
    {
        if (IsShipped)
            return Error.Conflict(
                "Order.AlreadyShipped",
                "Order has already been shipped.");

        if (_lines.Count == 0)
            return Error.Validation(
                "Order.EmptyOrder",
                "Cannot ship an order with no lines.");

        if (ShipperId == null)
            return Error.Validation(
                "Order.NoShipper",
                "Cannot ship an order without a shipper.");

        ShippedDate = shippedDate;
        return Result.Success();
    }
}