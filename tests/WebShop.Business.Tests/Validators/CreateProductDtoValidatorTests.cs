using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for CreateProductDtoValidator.
/// </summary>
[Trait("Category", "Unit")]
public class CreateProductDtoValidatorTests
{
    private readonly CreateProductDtoValidator _validator;

    public CreateProductDtoValidatorTests()
    {
        _validator = new CreateProductDtoValidator();
    }

    #region Name Tests

    [Fact]
    public void Name_Empty_ShouldHaveValidationError()
    {
        // Arrange
        CreateProductDto dto = new() { Name = string.Empty };

        // Act
        TestValidationResult<CreateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreateProductDto dto = new() { Name = new string('A', 501) };

        // Act
        TestValidationResult<CreateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Name_Valid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateProductDto dto = new() { Name = "Test Product" };

        // Act
        TestValidationResult<CreateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region LabelId Tests

    [Fact]
    public void LabelId_Zero_ShouldHaveValidationError()
    {
        // Arrange
        CreateProductDto dto = new() { Name = "Test", LabelId = 0 };

        // Act
        TestValidationResult<CreateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LabelId);
    }

    [Fact]
    public void LabelId_Negative_ShouldHaveValidationError()
    {
        // Arrange
        CreateProductDto dto = new() { Name = "Test", LabelId = -1 };

        // Act
        TestValidationResult<CreateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LabelId);
    }

    #endregion

    #region Gender Tests

    [Fact]
    public void Gender_InvalidValue_ShouldHaveValidationError()
    {
        // Arrange
        CreateProductDto dto = new() { Name = "Test", Gender = "invalid" };

        // Act
        TestValidationResult<CreateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public void Gender_ValidValue_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateProductDto dto = new() { Name = "Test", Gender = "unisex" };

        // Act
        TestValidationResult<CreateProductDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Gender);
    }

    #endregion
}
