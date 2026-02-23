using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.Business.Validators;

/// <summary>
/// Unit tests for ClearCacheByTagsRequestValidator.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class ClearCacheByTagsRequestValidatorTests
{
    private readonly ClearCacheByTagsRequestValidator _validator = new();

    #region Tags Required Tests

    [Fact]
    public async Task Tags_Null_ShouldHaveValidationError()
    {
        // Arrange
        ClearCacheByTagsRequest request = new() { Tags = null! };

        // Act
        TestValidationResult<ClearCacheByTagsRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tags);
    }

    [Fact]
    public async Task Tags_Empty_ShouldHaveValidationError()
    {
        // Arrange
        ClearCacheByTagsRequest request = new() { Tags = [] };

        // Act
        TestValidationResult<ClearCacheByTagsRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tags);
    }

    [Fact]
    public async Task Tags_WithValidItems_ShouldNotHaveValidationError()
    {
        // Arrange
        ClearCacheByTagsRequest request = new() { Tags = ["products", "labels"] };

        // Act
        TestValidationResult<ClearCacheByTagsRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Tags);
    }

    #endregion

    #region Tag Item Tests

    [Fact]
    public async Task Tags_ContainsEmptyString_ShouldHaveValidationError()
    {
        // Arrange
        ClearCacheByTagsRequest request = new() { Tags = ["valid-tag", ""] };

        // Act
        TestValidationResult<ClearCacheByTagsRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tags);
    }

    [Fact]
    public async Task Tags_ItemExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange - 256 chars exceeds 255
        ClearCacheByTagsRequest request = new() { Tags = [new string('a', 256)] };

        // Act
        TestValidationResult<ClearCacheByTagsRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Tags);
    }

    [Fact]
    public async Task Tags_ItemAtMaxLength_ShouldNotHaveValidationError()
    {
        // Arrange - exactly 255 chars
        ClearCacheByTagsRequest request = new() { Tags = [new string('a', 255)] };

        // Act
        TestValidationResult<ClearCacheByTagsRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Tags);
    }

    #endregion

    #region Valid Request Tests

    [Fact]
    public async Task Validate_ValidRequest_ShouldNotHaveAnyErrors()
    {
        // Arrange
        ClearCacheByTagsRequest request = new() { Tags = ["products", "labels", "colors"] };

        // Act
        TestValidationResult<ClearCacheByTagsRequest> result = await _validator.TestValidateAsync(request);

        // Assert
        result.Errors.Should().BeEmpty();
    }

    #endregion
}
