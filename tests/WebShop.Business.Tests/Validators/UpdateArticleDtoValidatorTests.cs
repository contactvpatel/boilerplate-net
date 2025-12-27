using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for UpdateArticleDtoValidator.
/// </summary>
[Trait("Category", "Unit")]
public class UpdateArticleDtoValidatorTests
{
    private readonly UpdateArticleDtoValidator _validator;

    public UpdateArticleDtoValidatorTests()
    {
        _validator = new UpdateArticleDtoValidator();
    }

    #region ProductId Tests

    [Fact]
    public void ProductId_Zero_ShouldHaveValidationError()
    {
        // Arrange
        UpdateArticleDto dto = new() { ProductId = 0 };

        // Act
        TestValidationResult<UpdateArticleDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    [Fact]
    public void ProductId_Negative_ShouldHaveValidationError()
    {
        // Arrange
        UpdateArticleDto dto = new() { ProductId = -1 };

        // Act
        TestValidationResult<UpdateArticleDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ProductId);
    }

    #endregion

    #region Ean Tests

    [Fact]
    public void Ean_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdateArticleDto dto = new() { Ean = new string('A', 51) };

        // Act
        TestValidationResult<UpdateArticleDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Ean);
    }

    #endregion

    #region TaxRate Tests

    [Fact]
    public void TaxRate_Exceeds100_ShouldHaveValidationError()
    {
        // Arrange
        UpdateArticleDto dto = new() { TaxRate = 101 };

        // Act
        TestValidationResult<UpdateArticleDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TaxRate);
    }

    #endregion

    #region DiscountInPercent Tests

    [Fact]
    public void DiscountInPercent_Exceeds100_ShouldHaveValidationError()
    {
        // Arrange
        UpdateArticleDto dto = new() { DiscountInPercent = 101 };

        // Act
        TestValidationResult<UpdateArticleDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DiscountInPercent);
    }

    #endregion
}
