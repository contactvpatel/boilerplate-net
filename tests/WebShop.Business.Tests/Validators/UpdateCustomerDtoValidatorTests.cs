using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for UpdateCustomerDtoValidator.
/// </summary>
[Trait("Category", "Unit")]
public class UpdateCustomerDtoValidatorTests
{
    private readonly UpdateCustomerDtoValidator _validator;

    public UpdateCustomerDtoValidatorTests()
    {
        _validator = new UpdateCustomerDtoValidator();
    }

    #region FirstName Tests

    [Fact]
    public void FirstName_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdateCustomerDto dto = new() { FirstName = new string('A', 201) };

        // Act
        TestValidationResult<UpdateCustomerDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void FirstName_Whitespace_ShouldHaveValidationError()
    {
        // Arrange
        UpdateCustomerDto dto = new() { FirstName = "   " };

        // Act
        TestValidationResult<UpdateCustomerDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public void FirstName_Valid_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateCustomerDto dto = new() { FirstName = "John" };

        // Act
        TestValidationResult<UpdateCustomerDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    #endregion

    #region Email Tests

    [Fact]
    public void Email_InvalidFormat_ShouldHaveValidationError()
    {
        // Arrange
        UpdateCustomerDto dto = new() { Email = "invalid-email" };

        // Act
        TestValidationResult<UpdateCustomerDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Email_Valid_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateCustomerDto dto = new() { Email = "test@example.com" };

        // Act
        TestValidationResult<UpdateCustomerDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Gender Tests

    [Fact]
    public void Gender_InvalidValue_ShouldHaveValidationError()
    {
        // Arrange
        UpdateCustomerDto dto = new() { Gender = "invalid" };

        // Act
        TestValidationResult<UpdateCustomerDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public void Gender_ValidValue_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateCustomerDto dto = new() { Gender = "female" };

        // Act
        TestValidationResult<UpdateCustomerDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Gender);
    }

    #endregion

    #region CurrentAddressId Tests

    [Fact]
    public void CurrentAddressId_Zero_ShouldHaveValidationError()
    {
        // Arrange
        UpdateCustomerDto dto = new() { CurrentAddressId = 0 };

        // Act
        TestValidationResult<UpdateCustomerDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentAddressId);
    }

    [Fact]
    public void CurrentAddressId_Negative_ShouldHaveValidationError()
    {
        // Arrange
        UpdateCustomerDto dto = new() { CurrentAddressId = -1 };

        // Act
        TestValidationResult<UpdateCustomerDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CurrentAddressId);
    }

    #endregion
}
