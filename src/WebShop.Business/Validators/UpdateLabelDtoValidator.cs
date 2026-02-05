using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for UpdateLabelDto.
/// </summary>
public class UpdateLabelDtoValidator : AbstractValidator<UpdateLabelDto>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateLabelDtoValidator"/> class.
    /// </summary>
    public UpdateLabelDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("Name must not exceed 200 characters.");

        RuleFor(x => x.SlugName)
            .MaximumLength(200)
            .When(x => !string.IsNullOrEmpty(x.SlugName))
            .WithMessage("Slug name must not exceed 200 characters.");
    }
}
