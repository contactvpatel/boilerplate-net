using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for UpdateStockDto.
/// </summary>
public class UpdateStockDtoValidator : AbstractValidator<UpdateStockDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateStockDtoValidator"/> class.
    /// </summary>
    public UpdateStockDtoValidator()
    {
        RuleFor(x => x.ArticleId)
            .GreaterThan(0)
            .When(x => x.ArticleId.HasValue)
            .WithMessage("Article ID must be greater than zero when provided.");

        RuleFor(x => x.Count)
            .GreaterThanOrEqualTo(0)
            .When(x => x.Count.HasValue)
            .WithMessage("Count must be greater than or equal to zero.");
    }
}
