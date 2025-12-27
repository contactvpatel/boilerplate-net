using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for CreateArticleDtoValidator.
/// </summary>
[Trait("Category", "Unit")]
public class CreateArticleDtoValidatorTests
{
    private readonly CreateArticleDtoValidator _validator;

    public CreateArticleDtoValidatorTests()
    {
        _validator = new CreateArticleDtoValidator();
    }

    #region ProductId Tests

    [Fact]
    public void ProductId_Zero_ShouldHaveValidationError()
    {
        // Arrange
        CreateArticleDto dto = new() { ProductId = 0 };

        // Act
        TestValidationResult<CreateArticleDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void ProductId_Negative_ShouldHaveValidationError()
    {
        // Arrange
        CreateArticleDto dto = new() { ProductId = -1 };

        // Act
        TestValidationResult<CreateArticleDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    #endregion

    #region Ean Tests

    [Fact]
    public void Ean_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreateArticleDto dto = new() { Ean = new string('A', 51) };

        // Act
        TestValidationResult<CreateArticleDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Ean);
    }

    #endregion

    #region TaxRate Tests

    [Fact]
    public void TaxRate_Exceeds100_ShouldHaveValidationError()
    {
        // Arrange
        CreateArticleDto dto = new() { TaxRate = 101 };

        // Act
        TestValidationResult<CreateArticleDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TaxRate);
    }

    [Fact]
    public void TaxRate_Negative_ShouldHaveValidationError()
    {
        // Arrange
        CreateArticleDto dto = new() { TaxRate = -1 };

        // Act
        TestValidationResult<CreateArticleDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TaxRate);
    }

    #endregion

    #region DiscountInPercent Tests

    [Fact]
    public void DiscountInPercent_Exceeds100_ShouldHaveValidationError()
    {
        // Arrange
        CreateArticleDto dto = new() { DiscountInPercent = 101 };

        // Act
        TestValidationResult<CreateArticleDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountInPercent);
    }

    #endregion
}
