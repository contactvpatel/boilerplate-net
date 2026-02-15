using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for UpdateSizeDtoValidator.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class UpdateSizeDtoValidatorTests
{
    private readonly UpdateSizeDtoValidator _validator = new();

    #region Gender Tests

    [Fact]
    public async Task Gender_ValidMale_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateSizeDto dto = new() { Gender = "male" };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public async Task Gender_ValidFemale_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateSizeDto dto = new() { Gender = "female" };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public async Task Gender_ValidUnisex_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateSizeDto dto = new() { Gender = "unisex" };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public async Task Gender_InvalidValue_ShouldHaveValidationError()
    {
        // Arrange
        UpdateSizeDto dto = new() { Gender = "invalid" };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public async Task Gender_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 21 chars exceeds 20
        UpdateSizeDto dto = new() { Gender = new string('a', 21) };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public async Task Gender_NullOrEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateSizeDto dto = new() { Gender = null };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Gender);
    }

    #endregion

    #region Category Tests

    [Fact]
    public async Task Category_WithinMaxLength_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateSizeDto dto = new() { Category = "shirts" };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Category);
    }

    [Fact]
    public async Task Category_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 101 chars exceeds 100
        UpdateSizeDto dto = new() { Category = new string('a', 101) };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Category);
    }

    #endregion

    #region SizeLabel Tests

    [Fact]
    public async Task SizeLabel_WithinMaxLength_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateSizeDto dto = new() { SizeLabel = "M" };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SizeLabel);
    }

    [Fact]
    public async Task SizeLabel_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 21 chars exceeds 20
        UpdateSizeDto dto = new() { SizeLabel = new string('X', 21) };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SizeLabel);
    }

    #endregion

    #region SizeUs, SizeUk, SizeEu Tests

    [Fact]
    public async Task SizeUs_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 51 chars exceeds 50
        UpdateSizeDto dto = new() { SizeUs = new string('a', 51) };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SizeUs);
    }

    [Fact]
    public async Task SizeUk_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 51 chars exceeds 50
        UpdateSizeDto dto = new() { SizeUk = new string('a', 51) };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SizeUk);
    }

    [Fact]
    public async Task SizeEu_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 51 chars exceeds 50
        UpdateSizeDto dto = new() { SizeEu = new string('a', 51) };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SizeEu);
    }

    #endregion

    #region Valid DTO Tests

    [Fact]
    public async Task ValidDto_ShouldNotHaveAnyErrors()
    {
        // Arrange
        UpdateSizeDto dto = new()
        {
            Gender = "male",
            Category = "shirts",
            SizeLabel = "M",
            SizeUs = "42",
            SizeUk = "42",
            SizeEu = "42"
        };

        // Act
        TestValidationResult<UpdateSizeDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.Errors.Should().BeEmpty();
    }

    #endregion
}
