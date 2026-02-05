using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for UpdateOrderDto.
/// </summary>
public class UpdateOrderDtoValidator : AbstractValidator<UpdateOrderDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateOrderDtoValidator"/> class.
    /// </summary>
    public UpdateOrderDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .When(x => x.CustomerId.HasValue)
            .WithMessage("Customer ID must be greater than zero when provided.");

        RuleFor(x => x.ShippingAddressId)
            .GreaterThan(0)
            .When(x => x.ShippingAddressId.HasValue)
            .WithMessage("Shipping address ID must be greater than zero when provided.");

        RuleFor(x => x.Total)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Total.HasValue)
            .WithMessage("Total must be greater than or equal to zero.");

        RuleFor(x => x.ShippingCost)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ShippingCost.HasValue)
            .WithMessage("Shipping cost must be greater than or equal to zero.");
    }
}
