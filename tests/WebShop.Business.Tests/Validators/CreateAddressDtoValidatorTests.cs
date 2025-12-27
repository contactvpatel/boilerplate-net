using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for CreateAddressDtoValidator.
/// </summary>
[Trait("Category", "Unit")]
public class CreateAddressDtoValidatorTests
{
    private readonly CreateAddressDtoValidator _validator;

    public CreateAddressDtoValidatorTests()
    {
        _validator = new CreateAddressDtoValidator();
    }

    #region CustomerId Tests

    [Fact]
    public void CustomerId_Zero_ShouldHaveValidationError()
    {
        // Arrange
        CreateAddressDto dto = new() { CustomerId = 0, Address1 = "123 Main St", City = "City", Zip = "12345" };

        // Act
        TestValidationResult<CreateAddressDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void CustomerId_Negative_ShouldHaveValidationError()
    {
        // Arrange
        CreateAddressDto dto = new() { CustomerId = -1, Address1 = "123 Main St", City = "City", Zip = "12345" };

        // Act
        TestValidationResult<CreateAddressDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    #endregion

    #region Address1 Tests

    [Fact]
    public void Address1_Empty_ShouldHaveValidationError()
    {
        // Arrange
        CreateAddressDto dto = new() { CustomerId = 1, Address1 = string.Empty, City = "City", Zip = "12345" };

        // Act
        TestValidationResult<CreateAddressDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address1);
    }

    [Fact]
    public void Address1_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreateAddressDto dto = new()
        {
            CustomerId = 1,
            Address1 = new string('A', 501),
            City = "City",
            Zip = "12345"
        };

        // Act
        TestValidationResult<CreateAddressDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address1);
    }

    #endregion

    #region City Tests

    [Fact]
    public void City_Empty_ShouldHaveValidationError()
    {
        // Arrange
        CreateAddressDto dto = new() { CustomerId = 1, Address1 = "123 Main St", City = string.Empty, Zip = "12345" };

        // Act
        TestValidationResult<CreateAddressDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.City);
    }

    #endregion

    #region Zip Tests

    [Fact]
    public void Zip_Empty_ShouldHaveValidationError()
    {
        // Arrange
        CreateAddressDto dto = new() { CustomerId = 1, Address1 = "123 Main St", City = "City", Zip = string.Empty };

        // Act
        TestValidationResult<CreateAddressDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Zip);
    }

    #endregion
}
