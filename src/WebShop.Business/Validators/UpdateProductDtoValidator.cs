using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for UpdateProductDto.
/// </summary>
public class UpdateProductDtoValidator : AbstractValidator<UpdateProductDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateProductDtoValidator"/> class.
    /// </summary>
    public UpdateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(500)
            .WithMessage("Product name must not exceed 500 characters.")
            .Must(name => string.IsNullOrEmpty(name) || !string.IsNullOrWhiteSpace(name))
            .When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("Product name cannot be empty or whitespace if provided.");

        RuleFor(x => x.LabelId)
            .GreaterThan(0)
            .When(x => x.LabelId.HasValue)
            .WithMessage("Label ID must be greater than zero when provided.");

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .WithMessage("Category must not exceed 100 characters.");

        RuleFor(x => x.Gender)
            .MaximumLength(20)
            .WithMessage("Gender must not exceed 20 characters.")
            .Must(gender => string.IsNullOrEmpty(gender) || gender.ToLowerInvariant() is "male" or "female" or "unisex")
            .WithMessage("Gender must be one of: male, female, or unisex.");
    }
}

