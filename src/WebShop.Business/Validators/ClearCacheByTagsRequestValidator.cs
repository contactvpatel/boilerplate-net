using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for ClearCacheByTagsRequest.
/// </summary>
public class ClearCacheByTagsRequestValidator : AbstractValidator<ClearCacheByTagsRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClearCacheByTagsRequestValidator"/> class.
    /// </summary>
    public ClearCacheByTagsRequestValidator()
    {
        RuleFor(x => x.Tags)
            .NotNull()
            .WithMessage("Tags list is required.")
            .NotEmpty()
            .WithMessage("At least one tag must be provided.");

        RuleForEach(x => x.Tags)
            .NotEmpty()
            .WithMessage("Tag cannot be empty.")
            .MaximumLength(255)
            .WithMessage("Tag must not exceed 255 characters.");
    }
}

