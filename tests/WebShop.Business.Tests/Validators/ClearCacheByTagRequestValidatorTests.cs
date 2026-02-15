using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for ClearCacheByTagRequestValidator.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class ClearCacheByTagRequestValidatorTests
{
    private readonly ClearCacheByTagRequestValidator _validator = new();

    #region Tag Required Tests

    [Fact]
    public async Task Tag_Empty_ShouldHaveValidationError()
    {
        // Arrange
        ClearCacheByTagRequest request = new() { Tag = "" };

        // Act
        TestValidationResult<ClearCacheByTagRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tag);
    }

    [Fact]
    public async Task Tag_Whitespace_ShouldHaveValidationError()
    {
        // Arrange - FluentValidation NotEmpty treats whitespace as empty by default
        ClearCacheByTagRequest request = new() { Tag = "   " };

        // Act
        TestValidationResult<ClearCacheByTagRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tag);
    }

    [Fact]
    public async Task Tag_Valid_ShouldNotHaveValidationError()
    {
        // Arrange
        ClearCacheByTagRequest request = new() { Tag = "products" };

        // Act
        TestValidationResult<ClearCacheByTagRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Tag);
    }

    #endregion

    #region Tag MaxLength Tests

    [Fact]
    public async Task Tag_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 256 chars exceeds 255
        ClearCacheByTagRequest request = new() { Tag = new string('a', 256) };

        // Act
        TestValidationResult<ClearCacheByTagRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tag);
    }

    [Fact]
    public async Task Tag_AtMaxLength_ShouldNotHaveValidationError()
    {
        // Arrange - exactly 255 chars
        ClearCacheByTagRequest request = new() { Tag = new string('a', 255) };

        // Act
        TestValidationResult<ClearCacheByTagRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Tag);
    }

    #endregion

    #region Valid Request Tests

    [Fact]
    public async Task ValidRequest_ShouldNotHaveAnyErrors()
    {
        // Arrange
        ClearCacheByTagRequest request = new() { Tag = "products" };

        // Act
        TestValidationResult<ClearCacheByTagRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.Errors.Should().BeEmpty();
    }

    #endregion
}
