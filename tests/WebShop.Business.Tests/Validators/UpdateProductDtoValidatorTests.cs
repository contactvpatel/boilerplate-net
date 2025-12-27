using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for UpdateProductDtoValidator.
/// </summary>
[Trait("Category", "Unit")]
public class UpdateProductDtoValidatorTests
{
    private readonly UpdateProductDtoValidator _validator;

    public UpdateProductDtoValidatorTests()
    {
        _validator = new UpdateProductDtoValidator();
    }

    #region Name Tests

    [Fact]
    public void Name_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdateProductDto dto = new() { Name = new string('A', 501) };

        // Act
        TestValidationResult<UpdateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_Whitespace_ShouldHaveValidationError()
    {
        // Arrange
        UpdateProductDto dto = new() { Name = "   " };

        // Act
        TestValidationResult<UpdateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_Valid_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateProductDto dto = new() { Name = "Updated Product" };

        // Act
        TestValidationResult<UpdateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region LabelId Tests

    [Fact]
    public void LabelId_Zero_ShouldHaveValidationError()
    {
        // Arrange
        UpdateProductDto dto = new() { LabelId = 0 };

        // Act
        TestValidationResult<UpdateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LabelId);
    }

    #endregion

    #region Gender Tests

    [Fact]
    public void Gender_InvalidValue_ShouldHaveValidationError()
    {
        // Arrange
        UpdateProductDto dto = new() { Gender = "invalid" };

        // Act
        TestValidationResult<UpdateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public void Gender_ValidValue_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateProductDto dto = new() { Gender = "male" };

        // Act
        TestValidationResult<UpdateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Gender);
    }

    #endregion
}
