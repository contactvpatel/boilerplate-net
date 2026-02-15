using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for ClearCacheByKeysRequestValidator.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class ClearCacheByKeysRequestValidatorTests
{
    private readonly ClearCacheByKeysRequestValidator _validator = new();

    #region Keys Required Tests

    [Fact]
    public async Task Keys_Null_ShouldHaveValidationError()
    {
        // Arrange
        ClearCacheByKeysRequest request = new() { Keys = null! };

        // Act
        TestValidationResult<ClearCacheByKeysRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Keys);
    }

    [Fact]
    public async Task Keys_Empty_ShouldHaveValidationError()
    {
        // Arrange
        ClearCacheByKeysRequest request = new() { Keys = [] };

        // Act
        TestValidationResult<ClearCacheByKeysRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Keys);
    }

    [Fact]
    public async Task Keys_WithValidItems_ShouldNotHaveValidationError()
    {
        // Arrange
        ClearCacheByKeysRequest request = new() { Keys = ["key1", "key2"] };

        // Act
        TestValidationResult<ClearCacheByKeysRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Keys);
    }

    #endregion

    #region Key Item Tests

    [Fact]
    public async Task Keys_ContainsEmptyString_ShouldHaveValidationError()
    {
        // Arrange
        ClearCacheByKeysRequest request = new() { Keys = ["valid-key", ""] };

        // Act
        TestValidationResult<ClearCacheByKeysRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Keys);
    }

    [Fact]
    public async Task Keys_ItemExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 1025 chars exceeds 1024
        ClearCacheByKeysRequest request = new() { Keys = [new string('a', 1025)] };

        // Act
        TestValidationResult<ClearCacheByKeysRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Keys);
    }

    [Fact]
    public async Task Keys_ItemAtMaxLength_ShouldNotHaveValidationError()
    {
        // Arrange - exactly 1024 chars
        ClearCacheByKeysRequest request = new() { Keys = [new string('a', 1024)] };

        // Act
        TestValidationResult<ClearCacheByKeysRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Keys);
    }

    #endregion

    #region Valid Request Tests

    [Fact]
    public async Task ValidRequest_ShouldNotHaveAnyErrors()
    {
        // Arrange
        ClearCacheByKeysRequest request = new() { Keys = ["cache-key-1", "cache-key-2"] };

        // Act
        TestValidationResult<ClearCacheByKeysRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.Errors.Should().BeEmpty();
    }

    #endregion
}
