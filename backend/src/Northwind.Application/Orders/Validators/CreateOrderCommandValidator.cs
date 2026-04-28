using FluentValidation;
using Northwind.Application.Orders.Commands;

namespace Northwind.Application.Orders.Validators;

/// <summary>
/// FluentValidation rules for CreateOrderCommand. These run before the command
/// reaches OrderService, catching obvious input errors at the API boundary
/// so the service layer only deals with business logic failures.
/// </summary>
public sealed class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer is required.");

        RuleFor(x => x.EmployeeId)
            .GreaterThan(0).WithMessage("Employee is required.");

        RuleFor(x => x.ShipName)
            .NotEmpty().WithMessage("Recipient name is required.");

        RuleFor(x => x.ShipStreet)
            .NotEmpty().WithMessage("Street address is required.");

        RuleFor(x => x.ShipCity)
            .NotEmpty().WithMessage("City is required.");

        RuleFor(x => x.ShipCountry)
            .NotEmpty().WithMessage("Country is required.");

        RuleFor(x => x.Freight)
            .GreaterThanOrEqualTo(0).WithMessage("Freight cannot be negative.");

        RuleFor(x => x.Lines)
            .NotEmpty().WithMessage("Order must have at least one line item.");

        RuleForEach(x => x.Lines).ChildRules(line =>
        {
            line.RuleFor(l => l.ProductId)
                .GreaterThan(0).WithMessage("Product is required.");

            line.RuleFor(l => l.Quantity)
                .GreaterThan((short)0).WithMessage("Quantity must be positive.");

            line.RuleFor(l => l.Discount)
                .InclusiveBetween(0f, 1f).WithMessage("Discount must be between 0 and 1.");
        });
    }
}