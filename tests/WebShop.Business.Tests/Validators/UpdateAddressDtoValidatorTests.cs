using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for UpdateAddressDtoValidator.
/// </summary>
[Trait("Category", "Unit")]
public class UpdateAddressDtoValidatorTests
{
    private readonly UpdateAddressDtoValidator _validator;

    public UpdateAddressDtoValidatorTests()
    {
        _validator = new UpdateAddressDtoValidator();
    }

    #region CustomerId Tests

    [Fact]
    public void CustomerId_Zero_ShouldHaveValidationError()
    {
        // Arrange
        UpdateAddressDto dto = new() { CustomerId = 0 };

        // Act
        TestValidationResult<UpdateAddressDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void CustomerId_Negative_ShouldHaveValidationError()
    {
        // Arrange
        UpdateAddressDto dto = new() { CustomerId = -1 };

        // Act
        TestValidationResult<UpdateAddressDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    #endregion

    #region Address1 Tests

    [Fact]
    public void Address1_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdateAddressDto dto = new() { Address1 = new string('A', 501) };

        // Act
        TestValidationResult<UpdateAddressDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address1);
    }

    #endregion

    #region City Tests

    [Fact]
    public void City_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdateAddressDto dto = new() { City = new string('A', 201) };

        // Act
        TestValidationResult<UpdateAddressDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.City);
    }

    #endregion

    #region Zip Tests

    [Fact]
    public void Zip_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        UpdateAddressDto dto = new() { Zip = new string('A', 21) };

        // Act
        TestValidationResult<UpdateAddressDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Zip);
    }

    #endregion
}
