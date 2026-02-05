using FluentValidation;
using WebShop.Api.Models;

namespace WebShop.Api.Validators;

/// <summary>
/// Validator for <see cref="PaginationQuery"/>.
/// PageSize must be between 1 and 100 when pagination is requested (Page &gt; 0).
/// </summary>
public class PaginationQueryValidator : AbstractValidator<PaginationQuery>
{
    /// <summary>
    /// Maximum allowed page size.
    /// </summary>
    public const int MaxPageSize = 100;

    /// <summary>
    /// Minimum allowed page size when pagination is used.
    /// </summary>
    public const int MinPageSize = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaginationQueryValidator"/> class.
    /// </summary>
    public PaginationQueryValidator()
    {
        When(p => p.IsPaginated, () => RuleFor(p => p.PageSize)
                .InclusiveBetween(MinPageSize, MaxPageSize)
                .WithMessage($"Page size must be between {MinPageSize} and {MaxPageSize} when pagination is requested."));
    }
}
