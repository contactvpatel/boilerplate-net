using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for UpdateArticleDto.
/// </summary>
public class UpdateArticleDtoValidator : AbstractValidator<UpdateArticleDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateArticleDtoValidator"/> class.
    /// </summary>
    public UpdateArticleDtoValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0)
            .When(x => x.ProductId.HasValue)
            .WithMessage("Product ID must be greater than zero when provided.");

        RuleFor(x => x.Ean)
            .MaximumLength(50)
            .WithMessage("EAN must not exceed 50 characters.");

        RuleFor(x => x.ColorId)
            .GreaterThan(0)
            .When(x => x.ColorId.HasValue)
            .WithMessage("Color ID must be greater than zero when provided.");

        RuleFor(x => x.Size)
            .GreaterThan(0)
            .When(x => x.Size.HasValue)
            .WithMessage("Size must be greater than zero when provided.");

        RuleFor(x => x.Description)
            .MaximumLength(1000)
            .WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.OriginalPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.OriginalPrice.HasValue)
            .WithMessage("Original price must be greater than or equal to zero.");

        RuleFor(x => x.ReducedPrice)
            .GreaterThanOrEqualTo(0)
            .When(x => x.ReducedPrice.HasValue)
            .WithMessage("Reduced price must be greater than or equal to zero.");

        RuleFor(x => x.TaxRate)
            .InclusiveBetween(0, 100)
            .When(x => x.TaxRate.HasValue)
            .WithMessage("Tax rate must be between 0 and 100.");

        RuleFor(x => x.DiscountInPercent)
            .InclusiveBetween(0, 100)
            .When(x => x.DiscountInPercent.HasValue)
            .WithMessage("Discount in percent must be between 0 and 100.");
    }
}
