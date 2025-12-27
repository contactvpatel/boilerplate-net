using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for UpdateAddressDto.
/// </summary>
public class UpdateAddressDtoValidator : AbstractValidator<UpdateAddressDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAddressDtoValidator"/> class.
    /// </summary>
    public UpdateAddressDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .When(x => x.CustomerId.HasValue)
            .WithMessage("Customer ID must be greater than zero.");

        RuleFor(x => x.FirstName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.FirstName))
            .WithMessage("First name must not exceed 200 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.LastName))
            .WithMessage("Last name must not exceed 200 characters.");

        RuleFor(x => x.Address1)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Address1))
            .WithMessage("Address line 1 must not exceed 500 characters.");

        RuleFor(x => x.Address2)
            .MaximumLength(500)
            .When(x => !string.IsNullOrEmpty(x.Address2))
            .WithMessage("Address line 2 must not exceed 500 characters.");

        RuleFor(x => x.City)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.City))
            .WithMessage("City must not exceed 200 characters.");

        RuleFor(x => x.Zip)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Zip))
            .WithMessage("ZIP code must not exceed 20 characters.");
    }
}

