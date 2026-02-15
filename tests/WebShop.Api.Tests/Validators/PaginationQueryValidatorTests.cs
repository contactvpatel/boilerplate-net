using FluentValidation.TestHelper;
using WebShop.Api.Models;
using WebShop.Api.Validators;
using Xunit;

namespace WebShop.Api.Tests.Validators;

/// <summary>
/// Unit tests for PaginationQueryValidator.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class PaginationQueryValidatorTests
{
    private readonly PaginationQueryValidator _validator;

    public PaginationQueryValidatorTests()
    {
        _validator = new PaginationQueryValidator();
    }

    [Fact]
    public void NonPaginated_PageZero_ShouldNotHaveValidationError()
    {
        // Arrange - Page 0 means non-paginated, PageSize is ignored
        PaginationQuery query = new() { Page = 0, PageSize = 0 };

        // Act
        TestValidationResult<PaginationQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Paginated_ValidPageSize_ShouldNotHaveValidationError()
    {
        // Arrange
        PaginationQuery query = new() { Page = 1, PageSize = 50 };

        // Act
        TestValidationResult<PaginationQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Paginated_PageSizeAtMin_ShouldNotHaveValidationError()
    {
        // Arrange
        PaginationQuery query = new() { Page = 1, PageSize = PaginationQueryValidator.MinPageSize };

        // Act
        TestValidationResult<PaginationQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Paginated_PageSizeAtMax_ShouldNotHaveValidationError()
    {
        // Arrange
        PaginationQuery query = new() { Page = 1, PageSize = PaginationQueryValidator.MaxPageSize };

        // Act
        TestValidationResult<PaginationQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Paginated_PageSizeZero_ShouldHaveValidationError()
    {
        // Arrange
        PaginationQuery query = new() { Page = 1, PageSize = 0 };

        // Act
        TestValidationResult<PaginationQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(p => p.PageSize);
    }

    [Fact]
    public void Paginated_PageSizeExceedsMax_ShouldHaveValidationError()
    {
        // Arrange
        PaginationQuery query = new() { Page = 1, PageSize = PaginationQueryValidator.MaxPageSize + 1 };

        // Act
        TestValidationResult<PaginationQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(p => p.PageSize);
    }

    [Fact]
    public void Paginated_PageSizeNegative_ShouldHaveValidationError()
    {
        // Arrange
        PaginationQuery query = new() { Page = 1, PageSize = -1 };

        // Act
        TestValidationResult<PaginationQuery> result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(p => p.PageSize);
    }
}
