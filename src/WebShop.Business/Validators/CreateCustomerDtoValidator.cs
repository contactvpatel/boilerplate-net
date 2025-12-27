using FluentValidation;
using WebShop.Business.DTOs;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for CreateCustomerDto with async database validation.
/// </summary>
public class CreateCustomerDtoValidator : AbstractValidator<CreateCustomerDto>
{
    private readonly ICustomerRepository _customerRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateCustomerDtoValidator"/> class.
    /// </summary>
    /// <param name="customerRepository">The customer repository for database validation.</param>
    public CreateCustomerDtoValidator(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository ?? throw new ArgumentNullException(nameof(customerRepository));

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
            .WithMessage("Email address must not exceed 255 characters.")
            .MustAsync(BeUniqueEmailAsync)
            .WithMessage("Email address is already in use. Please use a different email address.");

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

    /// <summary>
    /// Validates that the email address is unique in the database.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the email is unique, false if it already exists.</returns>
    private async Task<bool> BeUniqueEmailAsync(string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return true; // Let the NotEmpty rule handle this
        }

        Customer? existingCustomer = await _customerRepository.GetByEmailAsync(email, cancellationToken);
        return existingCustomer == null;
    }
}

