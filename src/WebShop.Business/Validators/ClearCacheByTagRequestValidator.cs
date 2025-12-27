using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for ClearCacheByTagRequest.
/// </summary>
public class ClearCacheByTagRequestValidator : AbstractValidator<ClearCacheByTagRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClearCacheByTagRequestValidator"/> class.
    /// </summary>
    public ClearCacheByTagRequestValidator()
    {
        RuleFor(x => x.Tag)
            .NotEmpty()
            .WithMessage("Tag is required.")
            .MaximumLength(255)
            .WithMessage("Tag must not exceed 255 characters.");
    }
}

