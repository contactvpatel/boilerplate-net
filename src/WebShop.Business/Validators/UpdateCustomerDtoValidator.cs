using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for UpdateCustomerDto.
/// </summary>
public class UpdateCustomerDtoValidator : AbstractValidator<UpdateCustomerDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCustomerDtoValidator"/> class.
    /// </summary>
    public UpdateCustomerDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .MaximumLength(200)
            .WithMessage("First name must not exceed 200 characters.")
            .Must(name => string.IsNullOrEmpty(name) || !string.IsNullOrWhiteSpace(name))
            .When(x => !string.IsNullOrEmpty(x.FirstName))
            .WithMessage("First name cannot be empty or whitespace if provided.");

        RuleFor(x => x.LastName)
            .MaximumLength(200)
            .WithMessage("Last name must not exceed 200 characters.")
            .Must(name => string.IsNullOrEmpty(name) || !string.IsNullOrWhiteSpace(name))
            .When(x => !string.IsNullOrEmpty(x.LastName))
            .WithMessage("Last name cannot be empty or whitespace if provided.");

        RuleFor(x => x.Email)
            .EmailAddress()
            .When(x => !string.IsNullOrWhiteSpace(x.Email))
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

        RuleFor(x => x.CurrentAddressId)
            .GreaterThan(0)
            .When(x => x.CurrentAddressId.HasValue)
            .WithMessage("Current address ID must be greater than zero when provided.");
    }
}

