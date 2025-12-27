using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for ClearCacheByKeysRequest.
/// </summary>
public class ClearCacheByKeysRequestValidator : AbstractValidator<ClearCacheByKeysRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClearCacheByKeysRequestValidator"/> class.
    /// </summary>
    public ClearCacheByKeysRequestValidator()
    {
        RuleFor(x => x.Keys)
            .NotNull()
            .WithMessage("Keys list is required.")
            .NotEmpty()
            .WithMessage("At least one cache key must be provided.");

        RuleForEach(x => x.Keys)
            .NotEmpty()
            .WithMessage("Cache key cannot be empty.")
            .MaximumLength(1024)
            .WithMessage("Cache key must not exceed 1024 characters.");
    }
}

