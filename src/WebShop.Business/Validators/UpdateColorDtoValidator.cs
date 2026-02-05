using System.Text.RegularExpressions;
using FluentValidation;
using WebShop.Business.DTOs;

namespace WebShop.Business.Validators;

/// <summary>
/// Validator for UpdateColorDto.
/// </summary>
public class UpdateColorDtoValidator : AbstractValidator<UpdateColorDto>
{
    private static readonly Regex RgbHexRegex = new(@"^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateColorDtoValidator"/> class.
    /// </summary>
    public UpdateColorDtoValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(100)
            .When(x => !string.IsNullOrEmpty(x.Name))
            .WithMessage("Name must not exceed 100 characters.");

        RuleFor(x => x.Rgb)
            .Must(rgb => RgbHexRegex.IsMatch(rgb!))
            .When(x => !string.IsNullOrEmpty(x.Rgb))
            .WithMessage("RGB must be in hex format (e.g., #FF0000).");
    }
}
