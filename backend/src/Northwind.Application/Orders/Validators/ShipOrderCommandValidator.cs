using FluentValidation;
using Northwind.Application.Orders.Commands;

namespace Northwind.Application.Orders.Validators;

/// <summary>
/// Validates ShipOrderCommand at the API boundary before it reaches OrderService.
/// The domain's MarkAsShipped also validates, but this catches obvious bad input early.
/// </summary>
public sealed class ShipOrderCommandValidator : AbstractValidator<ShipOrderCommand>
{
    public ShipOrderCommandValidator()
    {
        RuleFor(x => x.OrderId)
            .GreaterThan(0).WithMessage("Order ID must be positive.");

        RuleFor(x => x.ShipperId)
            .GreaterThan(0).WithMessage("Shipper is required to mark an order as shipped.");

        RuleFor(x => x.ShippedDate)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .When(x => x.ShippedDate.HasValue)
            .WithMessage("Shipped date cannot be in the future.");
    }
}