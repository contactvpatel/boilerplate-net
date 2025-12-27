using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for CreateAddressDto.
/// </summary>
public class CreateAddressDtoValidator : AbstractValidator<CreateAddressDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateAddressDtoValidator"/> class.
    /// </summary>
    public CreateAddressDtoValidator()
    {
        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("Customer ID must be greater than zero.");

        RuleFor(x => x.FirstName)
            .MaximumLength(200)
            .WithMessage("First name must not exceed 200 characters.");

        RuleFor(x => x.LastName)
            .MaximumLength(200)
            .WithMessage("Last name must not exceed 200 characters.");

        RuleFor(x => x.Address1)
            .NotEmpty()
            .WithMessage("Address line 1 is required.")
            .MaximumLength(500)
            .WithMessage("Address line 1 must not exceed 500 characters.");

        RuleFor(x => x.Address2)
            .MaximumLength(500)
            .WithMessage("Address line 2 must not exceed 500 characters.");

        RuleFor(x => x.City)
            .NotEmpty()
            .WithMessage("City is required.")
            .MaximumLength(200)
            .WithMessage("City must not exceed 200 characters.");

        RuleFor(x => x.Zip)
            .NotEmpty()
            .WithMessage("ZIP code is required.")
            .MaximumLength(20)
            .WithMessage("ZIP code must not exceed 20 characters.");
    }
}

