using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for CreateOrderDtoValidator.
/// </summary>
[Trait("Category", "Unit")]
public class CreateOrderDtoValidatorTests
{
    private readonly CreateOrderDtoValidator _validator;

    public CreateOrderDtoValidatorTests()
    {
        _validator = new CreateOrderDtoValidator();
    }

    #region CustomerId Tests

    [Fact]
    public void CustomerId_Zero_ShouldHaveValidationError()
    {
        // Arrange
        CreateOrderDto dto = new() { CustomerId = 0, ShippingAddressId = 1 };

        // Act
        TestValidationResult<CreateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void CustomerId_Negative_ShouldHaveValidationError()
    {
        // Arrange
        CreateOrderDto dto = new() { CustomerId = -1, ShippingAddressId = 1 };

        // Act
        TestValidationResult<CreateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    #endregion

    #region ShippingAddressId Tests

    [Fact]
    public void ShippingAddressId_Zero_ShouldHaveValidationError()
    {
        // Arrange
        CreateOrderDto dto = new() { CustomerId = 1, ShippingAddressId = 0 };

        // Act
        TestValidationResult<CreateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShippingAddressId);
    }

    [Fact]
    public void ShippingAddressId_Negative_ShouldHaveValidationError()
    {
        // Arrange
        CreateOrderDto dto = new() { CustomerId = 1, ShippingAddressId = -1 };

        // Act
        TestValidationResult<CreateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShippingAddressId);
    }

    #endregion

    #region ShippingCost Tests

    [Fact]
    public void ShippingCost_Negative_ShouldHaveValidationError()
    {
        // Arrange
        CreateOrderDto dto = new() { CustomerId = 1, ShippingAddressId = 1, ShippingCost = -1 };

        // Act
        TestValidationResult<CreateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShippingCost);
    }

    [Fact]
    public void ShippingCost_Zero_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateOrderDto dto = new() { CustomerId = 1, ShippingAddressId = 1, ShippingCost = 0 };

        // Act
        TestValidationResult<CreateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ShippingCost);
    }

    #endregion
}
