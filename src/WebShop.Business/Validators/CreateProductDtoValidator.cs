using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for CreateProductDto.
/// </summary>
public class CreateProductDtoValidator : AbstractValidator<CreateProductDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProductDtoValidator"/> class.
    /// </summary>
    public CreateProductDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Product name is required.")
            .MaximumLength(500)
            .WithMessage("Product name must not exceed 500 characters.");

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

