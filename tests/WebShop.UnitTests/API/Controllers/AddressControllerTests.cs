using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Controllers;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Models;
using WebShop.Business.Services.Interfaces;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.API.Controllers;

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
    public async Task GetAll_WhenCalled_ReturnsOkWithAddresses()
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

    [Fact]
    public async Task Create_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        CreateAddressDto createDto = new() { Address1 = "123 Main St", City = "Test City", Zip = "12345", CustomerId = 1 };
        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await controller.Create(createDto, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
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

    #region Patch Tests

    [Fact]
    public async Task Patch_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int addressId = 1;
        UpdateAddressDto patchDto = new() { City = "Patched City" };
        mockService.Setup(s => s.PatchAsync(addressId, patchDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<AddressDto>.Success(new AddressDto { Id = addressId }));

        // Act
        IActionResult result = await controller.Patch(addressId, patchDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Patch_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int addressId = 999;
        UpdateAddressDto patchDto = new() { City = "Patched City" };
        mockService.Setup(s => s.PatchAsync(addressId, patchDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<AddressDto>.NotFound());

        // Act
        IActionResult result = await controller.Patch(addressId, patchDto, CancellationToken.None);

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

    #region CreateBatch Tests

    [Fact]
    public async Task CreateBatch_ValidDtos_ReturnsCreated()
    {
        // Arrange
        List<CreateAddressDto> createDtos = new()
        {
            new() { CustomerId = 1, Address1 = "123 Main St", City = "City1", Zip = "12345" },
            new() { CustomerId = 1, Address1 = "456 Oak Ave", City = "City2", Zip = "67890" }
        };
        List<AddressDto> created = new()
        {
            new() { Id = 1, Address1 = "123 Main St", City = "City1" },
            new() { Id = 2, Address1 = "456 Oak Ave", City = "City2" }
        };
        mockService.Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<IReadOnlyList<AddressDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        (result.Result as ObjectResult)!.StatusCode.Should().Be(201);
        Response<IReadOnlyList<AddressDto>>? response = (result.Result as ObjectResult)!.Value as Response<IReadOnlyList<AddressDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CreateBatch_EmptyList_ReturnsCreatedWithEmptyList()
    {
        // Arrange
        List<CreateAddressDto> createDtos = new();
        mockService.Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AddressDto>());

        // Act
        ActionResult<Response<IReadOnlyList<AddressDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        (result.Result as ObjectResult)!.StatusCode.Should().Be(201);
        Response<IReadOnlyList<AddressDto>>? response = (result.Result as ObjectResult)!.Value as Response<IReadOnlyList<AddressDto>>;
        response!.Data.Should().BeEmpty();
        response.Succeeded.Should().BeTrue();
    }

    #endregion

    #region UpdateBatch Tests

    [Fact]
    public async Task UpdateBatch_ValidUpdates_ReturnsOk()
    {
        // Arrange
        List<BatchUpdateRequest<UpdateAddressDto>> updates = new()
        {
            new() { Id = 1, Data = new UpdateAddressDto { City = "City1" } },
            new() { Id = 2, Data = new UpdateAddressDto { City = "City2" } }
        };
        List<AddressDto> updated = new()
        {
            new() { Id = 1, City = "City1" },
            new() { Id = 2, City = "City2" }
        };
        mockService.Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateAddressDto UpdateDto)>>(), It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        // Act
        ActionResult<Response<IReadOnlyList<AddressDto>>> result = await controller.UpdateBatch(updates, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        Response<IReadOnlyList<AddressDto>>? response = (result.Result as OkObjectResult)!.Value as Response<IReadOnlyList<AddressDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBatch_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        List<BatchUpdateRequest<UpdateAddressDto>> updates = new();
        mockService.Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateAddressDto UpdateDto)>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<AddressDto>());

        // Act
        ActionResult<Response<IReadOnlyList<AddressDto>>> result = await controller.UpdateBatch(updates, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        Response<IReadOnlyList<AddressDto>>? response = (result.Result as OkObjectResult)!.Value as Response<IReadOnlyList<AddressDto>>;
        response!.Data.Should().BeEmpty();
        response.Succeeded.Should().BeTrue();
    }

    #endregion

    #region DeleteBatch Tests

    [Fact]
    public async Task DeleteBatch_ValidIds_ReturnsOk()
    {
        // Arrange
        List<int> ids = new() { 1, 2 };
        List<int> deletedIds = new() { 1, 2 };
        mockService.Setup(s => s.DeleteBatchAsync(ids, It.IsAny<CancellationToken>())).ReturnsAsync(deletedIds);

        // Act
        ActionResult<Response<IReadOnlyList<int>>> result = await controller.DeleteBatch(ids, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        Response<IReadOnlyList<int>>? response = (result.Result as OkObjectResult)!.Value as Response<IReadOnlyList<int>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatch_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        List<int> ids = new();
        mockService.Setup(s => s.DeleteBatchAsync(ids, It.IsAny<CancellationToken>())).ReturnsAsync(new List<int>());

        // Act
        ActionResult<Response<IReadOnlyList<int>>> result = await controller.DeleteBatch(ids, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        Response<IReadOnlyList<int>>? response = (result.Result as OkObjectResult)!.Value as Response<IReadOnlyList<int>>;
        response!.Data.Should().BeEmpty();
        response.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public async Task GetAll_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await controller.GetAll(CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Patch_ServiceThrowsException_PropagatesException()
    {
        // Arrange
        const int addressId = 1;
        UpdateAddressDto patchDto = new() { City = "Patched" };
        mockService.Setup(s => s.PatchAsync(addressId, patchDto, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await controller.Patch(addressId, patchDto, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
