using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for UpdateSizeDto.
/// </summary>
public class UpdateSizeDtoValidator : AbstractValidator<UpdateSizeDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateSizeDtoValidator"/> class.
    /// </summary>
    public UpdateSizeDtoValidator()
    {
        RuleFor(x => x.Gender)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.Gender))
            .WithMessage("Gender must not exceed 20 characters.")
            .Must(gender => string.IsNullOrEmpty(gender) || gender.ToLowerInvariant() is "male" or "female" or "unisex")
            .When(x => !string.IsNullOrEmpty(x.Gender))
            .WithMessage("Gender must be one of: male, female, or unisex.");

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Category))
            .WithMessage("Category must not exceed 100 characters.");

        RuleFor(x => x.SizeLabel)
            .MaximumLength(20)
            .When(x => !string.IsNullOrEmpty(x.SizeLabel))
            .WithMessage("Size label must not exceed 20 characters.");

        RuleFor(x => x.SizeUs)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.SizeUs))
            .WithMessage("Size US must not exceed 50 characters.");

        RuleFor(x => x.SizeUk)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.SizeUk))
            .WithMessage("Size UK must not exceed 50 characters.");

        RuleFor(x => x.SizeEu)
            .MaximumLength(50)
            .When(x => !string.IsNullOrEmpty(x.SizeEu))
            .WithMessage("Size EU must not exceed 50 characters.");
    }
}
