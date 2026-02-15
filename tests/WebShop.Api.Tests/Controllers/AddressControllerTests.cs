using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Controllers;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Models;
using WebShop.Business.Services.Interfaces;
using Xunit;

namespace WebShop.Api.Tests.Controllers;

/// <summary>
/// Unit tests for AddressController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class AddressControllerTests
{
    private readonly Mock<IAddressService> mockService = new();
    private readonly Mock<ILogger<AddressController>> mockLogger = new();
    private readonly AddressController controller;

    public AddressControllerTests()
    {
        controller = new AddressController(mockService.Object, mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithAddresses()
    {
        // Arrange
        List<AddressDto> addresses =
        [
            new() { Id = 1, Address1 = "123 Main St", City = "Test City" },
            new() { Id = 2, Address1 = "456 Oak Ave", City = "Test City" }
        ];
        mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(addresses);

        // Act
        ActionResult<Response<IReadOnlyList<AddressDto>>> result = await controller.GetAll(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<AddressDto>>? response = okResult!.Value as Response<IReadOnlyList<AddressDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsOkWithAddress()
    {
        // Arrange
        const int addressId = 1;
        AddressDto address = new() { Id = addressId, Address1 = "123 Main St", City = "Test City" };
        mockService.Setup(s => s.GetByIdAsync(addressId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<AddressDto>.Success(address));

        // Act
        ActionResult<Response<AddressDto>> result = await controller.GetById(addressId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<AddressDto>? response = okResult!.Value as Response<AddressDto>;
        response!.Data!.Id.Should().Be(addressId);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int addressId = 999;
        mockService.Setup(s => s.GetByIdAsync(addressId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<AddressDto>.NotFound());

        // Act
        ActionResult<Response<AddressDto>> result = await controller.GetById(addressId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByCustomerId Tests

    [Fact]
    public async Task GetByCustomerId_ValidCustomerId_ReturnsOkWithAddresses()
    {
        // Arrange
        const int customerId = 1;
        List<AddressDto> addresses = [new() { Id = 1, Address1 = "123 Main St", City = "Test City" }];
        mockService.Setup(s => s.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(addresses);

        // Act
        ActionResult<Response<IReadOnlyList<AddressDto>>> result = await controller.GetByCustomerId(customerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<AddressDto>>? response = okResult!.Value as Response<IReadOnlyList<AddressDto>>;
        response!.Data.Should().HaveCount(1);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        CreateAddressDto createDto = new() { Address1 = "123 Main St", City = "Test City", Zip = "12345", CustomerId = 1 };
        AddressDto created = new() { Id = 1, Address1 = "123 Main St", City = "Test City" };
        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<AddressDto>> result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        CreatedAtActionResult? createdResult = result.Result as CreatedAtActionResult;
        Response<AddressDto>? response = createdResult!.Value as Response<AddressDto>;
        response!.Data!.Id.Should().Be(1);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int addressId = 1;
        UpdateAddressDto updateDto = new() { Address1 = "456 Oak Ave", City = "Test City" };
        mockService.Setup(s => s.UpdateAsync(addressId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<AddressDto>.Success(new AddressDto { Id = addressId }));

        // Act
        IActionResult result = await controller.Update(addressId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int addressId = 999;
        UpdateAddressDto updateDto = new() { Address1 = "456 Oak Ave" };
        mockService.Setup(s => s.UpdateAsync(addressId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<AddressDto>.NotFound());

        // Act
        IActionResult result = await controller.Update(addressId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int addressId = 1;
        mockService.Setup(s => s.DeleteAsync(addressId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        IActionResult result = await controller.Delete(addressId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int addressId = 999;
        mockService.Setup(s => s.DeleteAsync(addressId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        IActionResult result = await controller.Delete(addressId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}
