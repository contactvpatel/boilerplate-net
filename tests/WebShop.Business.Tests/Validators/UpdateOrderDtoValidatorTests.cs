using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using Xunit;

namespace WebShop.Business.Tests.Validators;

/// <summary>
/// Unit tests for UpdateOrderDtoValidator.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class UpdateOrderDtoValidatorTests
{
    private readonly UpdateOrderDtoValidator _validator;

    public UpdateOrderDtoValidatorTests()
    {
        _validator = new UpdateOrderDtoValidator();
    }

    [Fact]
    public void ValidDto_ShouldNotHaveAnyErrors()
    {
        // Arrange
        UpdateOrderDto dto = new()
        {
            CustomerId = 1,
            ShippingAddressId = 1,
            Total = 100.50m,
            ShippingCost = 10m
        };

        // Act
        TestValidationResult<UpdateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void CustomerId_Zero_WhenProvided_ShouldHaveValidationError()
    {
        // Arrange
        UpdateOrderDto dto = new() { CustomerId = 0 };

        // Act
        TestValidationResult<UpdateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void CustomerId_Negative_WhenProvided_ShouldHaveValidationError()
    {
        // Arrange
        UpdateOrderDto dto = new() { CustomerId = -1 };

        // Act
        TestValidationResult<UpdateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void CustomerId_Null_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateOrderDto dto = new() { CustomerId = null };

        // Act
        TestValidationResult<UpdateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.CustomerId);
    }

    [Fact]
    public void ShippingAddressId_Zero_WhenProvided_ShouldHaveValidationError()
    {
        // Arrange
        UpdateOrderDto dto = new() { ShippingAddressId = 0 };

        // Act
        TestValidationResult<UpdateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShippingAddressId);
    }

    [Fact]
    public void ShippingAddressId_Negative_WhenProvided_ShouldHaveValidationError()
    {
        // Arrange
        UpdateOrderDto dto = new() { ShippingAddressId = -1 };

        // Act
        TestValidationResult<UpdateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShippingAddressId);
    }

    [Fact]
    public void Total_Negative_WhenProvided_ShouldHaveValidationError()
    {
        // Arrange
        UpdateOrderDto dto = new() { Total = -1 };

        // Act
        TestValidationResult<UpdateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Total);
    }

    [Fact]
    public void Total_Zero_WhenProvided_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateOrderDto dto = new() { Total = 0 };

        // Act
        TestValidationResult<UpdateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Total);
    }

    [Fact]
    public void ShippingCost_Negative_WhenProvided_ShouldHaveValidationError()
    {
        // Arrange
        UpdateOrderDto dto = new() { ShippingCost = -1 };

        // Act
        TestValidationResult<UpdateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ShippingCost);
    }

    [Fact]
    public void ShippingCost_Zero_WhenProvided_ShouldNotHaveValidationError()
    {
        // Arrange
        UpdateOrderDto dto = new() { ShippingCost = 0 };

        // Act
        TestValidationResult<UpdateOrderDto> result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ShippingCost);
    }
}
