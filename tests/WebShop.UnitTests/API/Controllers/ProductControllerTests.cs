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
/// Unit tests for ProductController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class ProductControllerTests
{
    private readonly Mock<IProductService> mockService = new();
    private readonly Mock<ILogger<ProductController>> mockLogger = new();
    private readonly ProductController controller;

    public ProductControllerTests()
    {
        controller = new ProductController(mockService.Object, mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_NonPaginated_ReturnsOkWithProducts()
    {
        // Arrange
        List<ProductDto> products =
        [
            new() { Id = 1, Name = "Product 1", Category = "Electronics" },
            new() { Id = 2, Name = "Product 2", Category = "Clothing" }
        ];
        mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        // Act
        IActionResult result = await controller.GetAll(new PaginationQuery(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result as OkObjectResult;
        Response<IReadOnlyList<ProductDto>>? response = okResult!.Value as Response<IReadOnlyList<ProductDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_Paginated_ReturnsPagedResult()
    {
        // Arrange
        List<ProductDto> products = [new() { Id = 1, Name = "Product 1", Category = "Electronics" }];
        mockService.Setup(s => s.GetPagedAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync((products, 50));

        // Act
        IActionResult result = await controller.GetAll(new PaginationQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

        // Assert - OkResponse wraps PagedResult in Response<T>
        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result as OkObjectResult;
        Response<PagedResult<ProductDto>>? response = okResult!.Value as Response<PagedResult<ProductDto>>;
        PagedResult<ProductDto>? pagedResult = response!.Data;
        pagedResult!.Items.Should().HaveCount(1);
        pagedResult.TotalCount.Should().Be(50);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsOkWithProduct()
    {
        // Arrange
        const int productId = 1;
        ProductDto product = new() { Id = productId, Name = "Test Product", Category = "Electronics" };
        mockService.Setup(s => s.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProductDto>.Success(product));

        // Act
        ActionResult<Response<ProductDto>> result = await controller.GetById(productId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<ProductDto>? response = okResult!.Value as Response<ProductDto>;
        response!.Data!.Id.Should().Be(productId);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int productId = 999;
        mockService.Setup(s => s.GetByIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProductDto>.NotFound());

        // Act
        ActionResult<Response<ProductDto>> result = await controller.GetById(productId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByCategory Tests

    [Fact]
    public async Task GetByCategory_ValidCategory_ReturnsOkWithProducts()
    {
        // Arrange
        const string category = "Electronics";
        List<ProductDto> products = [new() { Id = 1, Name = "Laptop", Category = category }];
        mockService.Setup(s => s.GetByCategoryAsync(category, It.IsAny<CancellationToken>())).ReturnsAsync(products);

        // Act
        ActionResult<Response<IReadOnlyList<ProductDto>>> result = await controller.GetByCategory(category, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<ProductDto>>? response = okResult!.Value as Response<IReadOnlyList<ProductDto>>;
        response!.Data.Should().HaveCount(1);
        response.Data![0].Category.Should().Be(category);
    }

    #endregion

    #region GetActive Tests

    [Fact]
    public async Task GetActive_WhenCalled_ReturnsOkWithActiveProducts()
    {
        // Arrange
        List<ProductDto> products = [new() { Id = 1, Name = "Active Product", Category = "Electronics" }];
        mockService.Setup(s => s.GetActiveProductsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(products);

        // Act
        ActionResult<Response<IReadOnlyList<ProductDto>>> result = await controller.GetActive(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<ProductDto>>? response = okResult!.Value as Response<IReadOnlyList<ProductDto>>;
        response!.Data.Should().HaveCount(1);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        CreateProductDto createDto = new() { Name = "New Product", Category = "Electronics" };
        ProductDto created = new() { Id = 1, Name = "New Product", Category = "Electronics" };
        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<ProductDto>> result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        CreatedAtActionResult? createdResult = result.Result as CreatedAtActionResult;
        Response<ProductDto>? response = createdResult!.Value as Response<ProductDto>;
        response!.Data!.Id.Should().Be(1);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int productId = 1;
        UpdateProductDto updateDto = new() { Name = "Updated Product", Category = "Electronics" };
        mockService.Setup(s => s.UpdateAsync(productId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProductDto>.Success(new ProductDto { Id = productId }));

        // Act
        IActionResult result = await controller.Update(productId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int productId = 999;
        UpdateProductDto updateDto = new() { Name = "Updated Product" };
        mockService.Setup(s => s.UpdateAsync(productId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ProductDto>.NotFound());

        // Act
        IActionResult result = await controller.Update(productId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int productId = 1;
        mockService.Setup(s => s.DeleteAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        IActionResult result = await controller.Delete(productId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int productId = 999;
        mockService.Setup(s => s.DeleteAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        IActionResult result = await controller.Delete(productId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateBatch Tests

    [Fact]
    public async Task CreateBatch_ValidDtos_ReturnsCreated()
    {
        // Arrange
        List<CreateProductDto> createDtos = [new() { Name = "Product 1", Category = "Electronics" }];
        List<ProductDto> created = [new() { Id = 1, Name = "Product 1", Category = "Electronics" }];
        mockService.Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<IReadOnlyList<ProductDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task CreateBatch_EmptyList_ReturnsCreatedWithEmptyList()
    {
        // Arrange - Service returns empty list for empty input
        List<CreateProductDto> createDtos = [];
        mockService.Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<ProductDto>());

        // Act
        ActionResult<Response<IReadOnlyList<ProductDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
        Response<IReadOnlyList<ProductDto>>? response = objectResult.Value as Response<IReadOnlyList<ProductDto>>;
        response!.Data.Should().BeEmpty();
    }

    #endregion

    #region UpdateBatch Tests

    [Fact]
    public async Task UpdateBatch_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        List<BatchUpdateRequest<UpdateProductDto>> updates = [];
        mockService.Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateProductDto UpdateDto)>>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<ProductDto>());

        // Act
        ActionResult<Response<IReadOnlyList<ProductDto>>> result = await controller.UpdateBatch(updates, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<ProductDto>>? response = okResult!.Value as Response<IReadOnlyList<ProductDto>>;
        response!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBatch_DuplicateIds_ReturnsOkWithUpdatedProducts()
    {
        // Arrange - Service handles duplicate IDs; last update wins per entity
        List<BatchUpdateRequest<UpdateProductDto>> updates =
        [
            new() { Id = 1, Data = new UpdateProductDto { Name = "First" } },
            new() { Id = 1, Data = new UpdateProductDto { Name = "Second" } }
        ];
        List<ProductDto> updated = [new() { Id = 1, Name = "Second", Category = "Electronics" }];
        mockService.Setup(s => s.UpdateBatchAsync(It.IsAny<IReadOnlyList<(int Id, UpdateProductDto UpdateDto)>>(), It.IsAny<CancellationToken>())).ReturnsAsync(updated);

        // Act
        ActionResult<Response<IReadOnlyList<ProductDto>>> result = await controller.UpdateBatch(updates, CancellationToken.None);

        // Assert - Controller returns 200; service handles duplicates (last write wins)
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<ProductDto>>? response = okResult!.Value as Response<IReadOnlyList<ProductDto>>;
        response!.Data.Should().HaveCount(1);
    }

    #endregion

    #region DeleteBatch Tests

    [Fact]
    public async Task DeleteBatch_EmptyList_ReturnsOkWithEmptyList()
    {
        // Arrange
        List<int> ids = [];
        mockService.Setup(s => s.DeleteBatchAsync(ids, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<int>());

        // Act
        ActionResult<Response<IReadOnlyList<int>>> result = await controller.DeleteBatch(ids, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<int>>? response = okResult!.Value as Response<IReadOnlyList<int>>;
        response!.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBatch_NegativeIds_ReturnsOkWithEmptyList()
    {
        // Arrange - Non-existent/negative IDs are silently skipped; service returns only successfully deleted IDs
        List<int> ids = [-1, -2];
        mockService.Setup(s => s.DeleteBatchAsync(ids, It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<int>());

        // Act
        ActionResult<Response<IReadOnlyList<int>>> result = await controller.DeleteBatch(ids, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<int>>? response = okResult!.Value as Response<IReadOnlyList<int>>;
        response!.Data.Should().BeEmpty();
    }

    #endregion
}
