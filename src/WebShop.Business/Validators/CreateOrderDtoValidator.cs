using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for CreateOrderDto.
/// </summary>
public class CreateOrderDtoValidator : AbstractValidator<CreateOrderDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateOrderDtoValidator"/> class.
    /// </summary>
    public CreateOrderDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("Customer ID must be greater than zero.");

        RuleFor(x => x.ShippingAddressId)
            .GreaterThan(0)
            .WithMessage("Shipping address ID must be greater than zero.");

        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0)
            .WithMessage("Shipping cost must be greater than or equal to zero.");
    }
}

