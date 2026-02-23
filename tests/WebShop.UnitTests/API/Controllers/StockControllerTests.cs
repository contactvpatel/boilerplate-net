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
/// Unit tests for StockController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class StockControllerTests
{
    private readonly Mock<IStockService> mockService = new();
    private readonly Mock<ILogger<StockController>> mockLogger = new();
    private readonly StockController controller;

    public StockControllerTests()
    {
        controller = new StockController(mockService.Object, mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_WhenCalled_ReturnsOkWithStockEntries()
    {
        // Arrange
        List<StockDto> stocks =
        [
            new() { Id = 1, ArticleId = 1, Count = 10 },
            new() { Id = 2, ArticleId = 2, Count = 5 }
        ];
        mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(stocks);

        // Act
        ActionResult<Response<IReadOnlyList<StockDto>>> result = await controller.GetAll(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<StockDto>>? response = okResult!.Value as Response<IReadOnlyList<StockDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsOkWithStockEntry()
    {
        // Arrange
        const int stockId = 1;
        StockDto stock = new() { Id = stockId, ArticleId = 1, Count = 10 };
        mockService.Setup(s => s.GetByIdAsync(stockId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<StockDto>.Success(stock));

        // Act
        ActionResult<Response<StockDto>> result = await controller.GetById(stockId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<StockDto>? response = okResult!.Value as Response<StockDto>;
        response!.Data!.Id.Should().Be(stockId);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int stockId = 999;
        mockService.Setup(s => s.GetByIdAsync(stockId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<StockDto>.NotFound());

        // Act
        ActionResult<Response<StockDto>> result = await controller.GetById(stockId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByArticleId Tests

    [Fact]
    public async Task GetByArticleId_ValidArticleId_ReturnsOkWithStockEntry()
    {
        // Arrange
        const int articleId = 1;
        StockDto stock = new() { Id = 1, ArticleId = articleId, Count = 10 };
        mockService.Setup(s => s.GetByArticleIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<StockDto>.Success(stock));

        // Act
        ActionResult<Response<StockDto>> result = await controller.GetByArticleId(articleId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<StockDto>? response = okResult!.Value as Response<StockDto>;
        response!.Data!.ArticleId.Should().Be(articleId);
    }

    [Fact]
    public async Task GetByArticleId_InvalidArticleId_ReturnsNotFound()
    {
        // Arrange
        const int articleId = 999;
        mockService.Setup(s => s.GetByArticleIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<StockDto>.NotFound());

        // Act
        ActionResult<Response<StockDto>> result = await controller.GetByArticleId(articleId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetLowStock Tests

    [Fact]
    public async Task GetLowStock_WhenCalled_ReturnsOkWithLowStockEntries()
    {
        // Arrange
        List<StockDto> stocks = [new() { Id = 1, ArticleId = 1, Count = 3 }];
        mockService.Setup(s => s.GetLowStockAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(stocks);

        // Act
        ActionResult<Response<IReadOnlyList<StockDto>>> result = await controller.GetLowStock(10, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<StockDto>>? response = okResult!.Value as Response<IReadOnlyList<StockDto>>;
        response!.Data.Should().HaveCount(1);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        CreateStockDto createDto = new() { ArticleId = 1, Count = 10 };
        StockDto created = new() { Id = 1, ArticleId = 1, Count = 10 };
        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<StockDto>> result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        CreatedAtActionResult? createdResult = result.Result as CreatedAtActionResult;
        Response<StockDto>? response = createdResult!.Value as Response<StockDto>;
        response!.Data!.Id.Should().Be(1);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int stockId = 1;
        UpdateStockDto updateDto = new() { Count = 20 };
        mockService.Setup(s => s.UpdateAsync(stockId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<StockDto>.Success(new StockDto { Id = stockId }));

        // Act
        IActionResult result = await controller.Update(stockId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int stockId = 999;
        UpdateStockDto updateDto = new() { Count = 20 };
        mockService.Setup(s => s.UpdateAsync(stockId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<StockDto>.NotFound());

        // Act
        IActionResult result = await controller.Update(stockId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int stockId = 1;
        mockService.Setup(s => s.DeleteAsync(stockId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        IActionResult result = await controller.Delete(stockId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int stockId = 999;
        mockService.Setup(s => s.DeleteAsync(stockId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        IActionResult result = await controller.Delete(stockId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateBatch Tests

    [Fact]
    public async Task CreateBatch_ValidDtos_ReturnsCreated()
    {
        // Arrange
        List<CreateStockDto> createDtos = [new() { ArticleId = 1, Count = 10 }];
        List<StockDto> created = [new() { Id = 1, ArticleId = 1, Count = 10 }];
        mockService.Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<IReadOnlyList<StockDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateBatch_EmptyList_ReturnsCreatedWithEmptyList()
    {
        List<CreateStockDto> createDtos = [];
        mockService.Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>())).ReturnsAsync(new List<StockDto>());

        ActionResult<Response<IReadOnlyList<StockDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        result.Result.Should().BeOfType<ObjectResult>();
        (result.Result as ObjectResult)!.StatusCode.Should().Be(201);
        ((result.Result as ObjectResult)!.Value as Response<IReadOnlyList<StockDto>>)!.Data.Should().BeEmpty();
    }

    #endregion

    #region Patch Tests

    [Fact]
    public async Task Patch_ValidId_ReturnsNoContent()
    {
        const int stockId = 1;
        UpdateStockDto patchDto = new() { Count = 25 };
        mockService.Setup(s => s.PatchAsync(stockId, patchDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<StockDto>.Success(new StockDto { Id = stockId }));

        IActionResult result = await controller.Patch(stockId, patchDto, CancellationToken.None);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Patch_InvalidId_ReturnsNotFound()
    {
        const int stockId = 999;
        UpdateStockDto patchDto = new() { Count = 25 };
        mockService.Setup(s => s.PatchAsync(stockId, patchDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<StockDto>.NotFound());

        IActionResult result = await controller.Patch(stockId, patchDto, CancellationToken.None);

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region UpdateBatch Tests

    [Fact]
    public async Task UpdateBatch_ValidUpdates_ReturnsOk()
    {
        List<BatchUpdateRequest<UpdateStockDto>> updates = [new() { Id = 1, Data = new UpdateStockDto { Count = 50 } }];
        List<StockDto> updated = [new() { Id = 1, ArticleId = 1, Count = 50 }];
        mockService.Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateStockDto UpdateDto)>>(), It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        ActionResult<Response<IReadOnlyList<StockDto>>> result = await controller.UpdateBatch(updates, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        ((result.Result as OkObjectResult)!.Value as Response<IReadOnlyList<StockDto>>)!.Data.Should().HaveCount(1);
    }

    [Fact]
    public async Task UpdateBatch_EmptyList_ReturnsOkWithEmptyList()
    {
        List<BatchUpdateRequest<UpdateStockDto>> updates = [];
        mockService.Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateStockDto UpdateDto)>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<StockDto>());

        ActionResult<Response<IReadOnlyList<StockDto>>> result = await controller.UpdateBatch(updates, CancellationToken.None);

        result.Result.Should().BeOfType<OkObjectResult>();
        ((result.Result as OkObjectResult)!.Value as Response<IReadOnlyList<StockDto>>)!.Data.Should().BeEmpty();
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
        CreateStockDto createDto = new() { ArticleId = 1, Count = 10 };
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
