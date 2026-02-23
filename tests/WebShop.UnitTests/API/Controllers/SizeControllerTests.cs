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
/// Unit tests for SizeController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class SizeControllerTests
{
    private readonly Mock<ISizeService> mockService = new();
    private readonly Mock<ILogger<SizeController>> mockLogger = new();
    private readonly SizeController controller;

    public SizeControllerTests()
    {
        controller = new SizeController(mockService.Object, mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithSizes()
    {
        // Arrange
        List<SizeDto> sizes =
        [
            new() { Id = 1, Gender = "male", Category = "shirts", SizeLabel = "M" },
            new() { Id = 2, Gender = "female", Category = "pants", SizeLabel = "S" }
        ];
        mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(sizes);

        // Act
        ActionResult<Response<IReadOnlyList<SizeDto>>> result = await controller.GetAll(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<SizeDto>>? response = okResult!.Value as Response<IReadOnlyList<SizeDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsOkWithSize()
    {
        // Arrange
        const int sizeId = 1;
        SizeDto size = new() { Id = sizeId, Gender = "male", Category = "shirts", SizeLabel = "M" };
        mockService.Setup(s => s.GetByIdAsync(sizeId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<SizeDto>.Success(size));

        // Act
        ActionResult<Response<SizeDto>> result = await controller.GetById(sizeId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<SizeDto>? response = okResult!.Value as Response<SizeDto>;
        response!.Data!.Id.Should().Be(sizeId);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int sizeId = 999;
        mockService.Setup(s => s.GetByIdAsync(sizeId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<SizeDto>.NotFound());

        // Act
        ActionResult<Response<SizeDto>> result = await controller.GetById(sizeId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByGenderAndCategory Tests

    [Fact]
    public async Task GetByGenderAndCategory_ValidParams_ReturnsOkWithSizes()
    {
        // Arrange
        const string gender = "male";
        const string category = "shirts";
        List<SizeDto> sizes = [new() { Id = 1, Gender = gender, Category = category, SizeLabel = "M" }];
        mockService.Setup(s => s.GetByGenderAndCategoryAsync(gender, category, It.IsAny<CancellationToken>())).ReturnsAsync(sizes);

        // Act
        ActionResult<Response<IReadOnlyList<SizeDto>>> result = await controller.GetByGenderAndCategory(gender, category, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<SizeDto>>? response = okResult!.Value as Response<IReadOnlyList<SizeDto>>;
        response!.Data.Should().HaveCount(1);
        response.Data![0].Gender.Should().Be(gender);
        response.Data[0].Category.Should().Be(category);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        CreateSizeDto createDto = new() { Gender = "male", Category = "shirts", SizeLabel = "M" };
        SizeDto created = new() { Id = 1, Gender = "male", Category = "shirts", SizeLabel = "M" };
        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<SizeDto>> result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        CreatedAtActionResult? createdResult = result.Result as CreatedAtActionResult;
        Response<SizeDto>? response = createdResult!.Value as Response<SizeDto>;
        response!.Data!.Id.Should().Be(1);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int sizeId = 1;
        UpdateSizeDto updateDto = new() { SizeLabel = "L", SizeUs = "42" };
        mockService.Setup(s => s.UpdateAsync(sizeId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<SizeDto>.Success(new SizeDto { Id = sizeId }));

        // Act
        IActionResult result = await controller.Update(sizeId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int sizeId = 999;
        UpdateSizeDto updateDto = new() { SizeLabel = "L" };
        mockService.Setup(s => s.UpdateAsync(sizeId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<SizeDto>.NotFound());

        // Act
        IActionResult result = await controller.Update(sizeId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int sizeId = 1;
        mockService.Setup(s => s.DeleteAsync(sizeId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        IActionResult result = await controller.Delete(sizeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int sizeId = 999;
        mockService.Setup(s => s.DeleteAsync(sizeId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        IActionResult result = await controller.Delete(sizeId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateBatch Tests

    [Fact]
    public async Task CreateBatch_ValidDtos_ReturnsCreated()
    {
        // Arrange
        List<CreateSizeDto> createDtos = [new() { Gender = "male", Category = "shirts", SizeLabel = "M" }];
        List<SizeDto> created = [new() { Id = 1, Gender = "male", Category = "shirts", SizeLabel = "M" }];
        mockService.Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<IReadOnlyList<SizeDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateBatch_EmptyList_ReturnsCreatedWithEmptyList()
    {
        List<CreateSizeDto> createDtos = [];
        mockService.Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>())).ReturnsAsync(new List<SizeDto>());

        ActionResult<Response<IReadOnlyList<SizeDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>();
        (result.Result as ObjectResult)!.StatusCode.Should().Be(201);
        ((result.Result as ObjectResult)!.Value as Response<IReadOnlyList<SizeDto>>)!.Data.Should().BeEmpty();
    }

    #endregion

    #region Patch Tests

    [Fact]
    public async Task Patch_ValidId_ReturnsNoContent()
    {
        const int sizeId = 1;
        UpdateSizeDto patchDto = new() { SizeLabel = "L" };
        mockService.Setup(s => s.PatchAsync(sizeId, patchDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<SizeDto>.Success(new SizeDto { Id = sizeId }));

        IActionResult result = await controller.Patch(sizeId, patchDto, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Patch_InvalidId_ReturnsNotFound()
    {
        const int sizeId = 999;
        UpdateSizeDto patchDto = new() { SizeLabel = "L" };
        mockService.Setup(s => s.PatchAsync(sizeId, patchDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<SizeDto>.NotFound());

        IActionResult result = await controller.Patch(sizeId, patchDto, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region UpdateBatch Tests

    [Fact]
    public async Task UpdateBatch_ValidUpdates_ReturnsOk()
    {
        List<BatchUpdateRequest<UpdateSizeDto>> updates = [new() { Id = 1, Data = new UpdateSizeDto { SizeLabel = "L" } }];
        List<SizeDto> updated = [new() { Id = 1, SizeLabel = "L" }];
        mockService.Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateSizeDto UpdateDto)>>(), It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        ActionResult<Response<IReadOnlyList<SizeDto>>> result = await controller.UpdateBatch(updates, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        ((result.Result as OkObjectResult)!.Value as Response<IReadOnlyList<SizeDto>>)!.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateBatch_EmptyList_ReturnsOkWithEmptyList()
    {
        List<BatchUpdateRequest<UpdateSizeDto>> updates = [];
        mockService.Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateSizeDto UpdateDto)>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<SizeDto>());

        ActionResult<Response<IReadOnlyList<SizeDto>>> result = await controller.UpdateBatch(updates, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        ((result.Result as OkObjectResult)!.Value as Response<IReadOnlyList<SizeDto>>)!.Data.Should().BeEmpty();
    }

    #endregion

    #region DeleteBatch Tests

    [Fact]
    public async Task DeleteBatch_ValidIds_ReturnsOk()
    {
        List<int> ids = [1, 2];
        List<int> deletedIds = [1, 2];
        mockService.Setup(s => s.DeleteBatchAsync(ids, It.IsAny<CancellationToken>())).ReturnsAsync(deletedIds);

        ActionResult<Response<IReadOnlyList<int>>> result = await controller.DeleteBatch(ids, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        ((result.Result as OkObjectResult)!.Value as Response<IReadOnlyList<int>>)!.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteBatch_EmptyList_ReturnsOkWithEmptyList()
    {
        List<int> ids = [];
        mockService.Setup(s => s.DeleteBatchAsync(ids, It.IsAny<CancellationToken>())).ReturnsAsync(new List<int>());

        ActionResult<Response<IReadOnlyList<int>>> result = await controller.DeleteBatch(ids, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        ((result.Result as OkObjectResult)!.Value as Response<IReadOnlyList<int>>)!.Data.Should().BeEmpty();
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public async Task Create_ServiceThrowsException_PropagatesException()
    {
        CreateSizeDto createDto = new() { Gender = "male", Category = "Apparel", SizeLabel = "M" };
        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Database error"));

        Func<Task> act = async () => await controller.Create(createDto, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAll_ServiceThrowsException_PropagatesException()
    {
        mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("Database error"));

        Func<Task> act = async () => await controller.GetAll(CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
