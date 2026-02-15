using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for UpdateStockDtoValidator.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class UpdateStockDtoValidatorTests
{
    private readonly UpdateStockDtoValidator _validator = new();

    #region ArticleId Tests

    [Fact]
    public async Task ArticleId_Zero_ShouldHaveValidationError()
    {
        // Arrange
        UpdateStockDto dto = new() { ArticleId = 0 };

        // Act
        TestValidationResult<UpdateStockDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ArticleId);
    }

    [Fact]
    public async Task ArticleId_Negative_ShouldHaveValidationError()
    {
        // Arrange
        UpdateStockDto dto = new() { ArticleId = -1 };

        // Act
        TestValidationResult<UpdateStockDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ArticleId);
    }

    [Fact]
    public async Task ArticleId_Valid_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateStockDto dto = new() { ArticleId = 1 };

        // Act
        TestValidationResult<UpdateStockDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ArticleId);
    }

    [Fact]
    public async Task ArticleId_Null_ShouldNotHaveValidationError()
    {
        // Arrange - When ArticleId is null, the rule is not applied (When clause)
        UpdateStockDto dto = new() { ArticleId = null };

        // Act
        TestValidationResult<UpdateStockDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ArticleId);
    }

    #endregion

    #region Count Tests

    [Fact]
    public async Task Count_Negative_ShouldHaveValidationError()
    {
        // Arrange
        UpdateStockDto dto = new() { Count = -1 };

        // Act
        TestValidationResult<UpdateStockDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public async Task Count_Zero_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateStockDto dto = new() { Count = 0 };

        // Act
        TestValidationResult<UpdateStockDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public async Task Count_Positive_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateStockDto dto = new() { Count = 10 };

        // Act
        TestValidationResult<UpdateStockDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Count);
    }

    [Fact]
    public async Task Count_Null_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateStockDto dto = new() { Count = null };

        // Act
        TestValidationResult<UpdateStockDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Count);
    }

    #endregion
}
