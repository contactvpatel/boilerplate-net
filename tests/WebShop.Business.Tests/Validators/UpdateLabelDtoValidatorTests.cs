using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for UpdateLabelDtoValidator.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class UpdateLabelDtoValidatorTests
{
    private readonly UpdateLabelDtoValidator _validator = new();

    #region Name Tests

    [Fact]
    public async Task Name_WithinMaxLength_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateLabelDto dto = new() { Name = "Brand Name" };

        // Act
        TestValidationResult<UpdateLabelDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Name_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 201 chars exceeds 200
        UpdateLabelDto dto = new() { Name = new string('a', 201) };

        // Act
        TestValidationResult<UpdateLabelDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Name_NullOrEmpty_ShouldNotHaveValidationError()
    {
        // Arrange - When clause: only validates when not null/empty
        UpdateLabelDto dto = new() { Name = null };

        // Act
        TestValidationResult<UpdateLabelDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region SlugName Tests

    [Fact]
    public async Task SlugName_WithinMaxLength_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateLabelDto dto = new() { SlugName = "brand-slug" };

        // Act
        TestValidationResult<UpdateLabelDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SlugName);
    }

    [Fact]
    public async Task SlugName_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 201 chars exceeds 200
        UpdateLabelDto dto = new() { SlugName = new string('a', 201) };

        // Act
        TestValidationResult<UpdateLabelDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.SlugName);
    }

    [Fact]
    public async Task SlugName_NullOrEmpty_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateLabelDto dto = new() { SlugName = null };

        // Act
        TestValidationResult<UpdateLabelDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.SlugName);
    }

    #endregion

    #region Valid DTO Tests

    [Fact]
    public async Task ValidDto_ShouldNotHaveAnyErrors()
    {
        // Arrange
        UpdateLabelDto dto = new() { Name = "Brand A", SlugName = "brand-a" };

        // Act
        TestValidationResult<UpdateLabelDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.Errors.Should().BeEmpty();
    }

    #endregion
}
