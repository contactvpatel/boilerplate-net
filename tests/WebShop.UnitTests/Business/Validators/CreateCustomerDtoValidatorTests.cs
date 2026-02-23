using FluentAssertions;
using FluentValidation.TestHelper;
using WebShop.Business.DTOs;
using WebShop.Business.Validators;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.Business.Validators;

/// <summary>
/// Unit tests for CreateCustomerDtoValidator.
/// Email uniqueness is enforced in CustomerService; validator only checks format and length.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class CreateCustomerDtoValidatorTests
{
    private readonly CreateCustomerDtoValidator _validator = new();

    #region FirstName Tests

    [Fact]
    public async Task FirstName_Empty_ShouldHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new() { FirstName = string.Empty, LastName = "Doe", Email = "test@example.com" };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public async Task FirstName_ExceedsMaxLength_ShouldHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new()
        {
            FirstName = new string('A', 201),
            LastName = "Doe",
            Email = "test@example.com"
        };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName);
    }

    [Fact]
    public async Task FirstName_Valid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new() { FirstName = "John", LastName = "Doe", Email = "test@example.com" };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.FirstName);
    }

    #endregion

    #region LastName Tests

    [Fact]
    public async Task LastName_Empty_ShouldHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new() { FirstName = "John", LastName = string.Empty, Email = "test@example.com" };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName);
    }

    [Fact]
    public async Task LastName_Valid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new() { FirstName = "John", LastName = "Doe", Email = "test@example.com" };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.LastName);
    }

    #endregion

    #region Email Tests

    [Fact]
    public async Task Email_Empty_ShouldHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new() { FirstName = "John", LastName = "Doe", Email = string.Empty };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Email_InvalidFormat_ShouldHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new() { FirstName = "John", LastName = "Doe", Email = "invalid-email" };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public async Task Email_Valid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new() { FirstName = "John", LastName = "Doe", Email = "test@example.com" };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    #endregion

    #region Gender Tests

    [Fact]
    public async Task Gender_InvalidValue_ShouldHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new()
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "test@example.com",
            Gender = "invalid"
        };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public async Task Gender_ValidValue_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new()
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "test@example.com",
            Gender = "male"
        };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Gender);
    }

    #endregion

    #region DateOfBirth Tests

    [Fact]
    public async Task DateOfBirth_FutureDate_ShouldHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new()
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "test@example.com",
            DateOfBirth = DateTime.UtcNow.AddDays(1)
        };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public async Task DateOfBirth_Under13YearsOld_ShouldHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new()
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "test@example.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-10)
        };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Fact]
    public async Task DateOfBirth_Valid_ShouldNotHaveValidationError()
    {
        // Arrange
        CreateCustomerDto dto = new()
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "test@example.com",
            DateOfBirth = DateTime.UtcNow.AddYears(-20)
        };

        // Act
        TestValidationResult<CreateCustomerDto> result = await _validator.TestValidateAsync(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);
    }

    #endregion
}
