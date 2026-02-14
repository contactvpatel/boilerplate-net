using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for CreateCustomerDto. Validates format and length only.
/// Email uniqueness is enforced in CustomerService.CreateAsync to avoid coupling validation to data access.
/// </summary>
public class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
{
    public CreateCustomerDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required.")
            .MaximumLength(200)
            .WithMessage("First name must not exceed 200 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required.")
            .MaximumLength(200)
            .WithMessage("Last name must not exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email address is required.")
            .EmailAddress()
            .WithMessage("Email address must be in a valid format.")
            .MaximumLength(255)
            .WithMessage("Email address must not exceed 255 characters.");

        RuleFor(x => x.Gender)
            .MaximumLength(20)
            .WithMessage("Gender must not exceed 20 characters.")
            .Must(gender => string.IsNullOrEmpty(gender) || gender.ToLowerInvariant() is "male" or "female" or "unisex")
            .WithMessage("Gender must be one of: male, female, or unisex.");

        RuleFor(x => x.DateOfBirth)
            .LessThan(DateTime.UtcNow)
            .When(x => x.DateOfBirth.HasValue)
            .WithMessage("Date of birth must be in the past.")
            .Must(dob => !dob.HasValue || dob.Value <= DateTime.UtcNow.AddYears(-13))
            .When(x => x.DateOfBirth.HasValue)
            .WithMessage("Customer must be at least 13 years old.");
    }
}

