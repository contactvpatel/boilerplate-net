using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.Business.Validators;

/// <summary>
/// Unit tests for UpdateColorDtoValidator.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class UpdateColorDtoValidatorTests
{
    private readonly UpdateColorDtoValidator _validator = new();

    #region Name Tests

    [Fact]
    public async Task Name_WithinMaxLength_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateColorDto dto = new() { Name = "Red" };

        // Act
        TestValidationResult<UpdateColorDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Name_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 101 chars exceeds 100
        UpdateColorDto dto = new() { Name = new string('a', 101) };

        // Act
        TestValidationResult<UpdateColorDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Name_NullOrEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateColorDto dto = new() { Name = null };

        // Act
        TestValidationResult<UpdateColorDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Rgb Tests

    [Fact]
    public async Task Rgb_ValidHex_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateColorDto dto = new() { Rgb = "#FF0000" };

        // Act
        TestValidationResult<UpdateColorDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Rgb);
    }

    [Fact]
    public async Task Rgb_LowercaseHex_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateColorDto dto = new() { Rgb = "#ff0000" };

        // Act
        TestValidationResult<UpdateColorDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Rgb);
    }

    [Fact]
    public async Task Rgb_InvalidFormat_ShouldHaveValidationError()
    {
        // Arrange - missing # or wrong length
        UpdateColorDto dto = new() { Rgb = "FF0000" };

        // Act
        TestValidationResult<UpdateColorDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rgb);
    }

    [Fact]
    public async Task Rgb_InvalidHexChars_ShouldHaveValidationError()
    {
        // Arrange - G is not valid hex
        UpdateColorDto dto = new() { Rgb = "#GG0000" };

        // Act
        TestValidationResult<UpdateColorDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rgb);
    }

    [Fact]
    public async Task Rgb_TooShort_ShouldHaveValidationError()
    {
        // Arrange - 5 hex digits instead of 6
        UpdateColorDto dto = new() { Rgb = "#FF000" };

        // Act
        TestValidationResult<UpdateColorDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rgb);
    }

    [Fact]
    public async Task Rgb_NullOrEmpty_ShouldNotHaveValidationError()
    {
        // Arrange - When clause: only validates when not null/empty
        UpdateColorDto dto = new() { Rgb = null };

        // Act
        TestValidationResult<UpdateColorDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Rgb);
    }

    #endregion

    #region Valid DTO Tests

    [Fact]
    public async Task Validate_ValidDto_ShouldNotHaveAnyErrors()
    {
        // Arrange
        UpdateColorDto dto = new() { Name = "Red", Rgb = "#FF0000" };

        // Act
        TestValidationResult<UpdateColorDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.Errors.Should().BeEmpty();
    }

    #endregion
}
