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
/// Unit tests for ArticleController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class ArticleControllerTests
{
    private readonly Mock<IArticleService> mockService = new();
    private readonly Mock<ILogger<ArticleController>> mockLogger = new();
    private readonly ArticleController controller;

    public ArticleControllerTests()
    {
        controller = new ArticleController(mockService.Object, mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_ReturnsOkWithArticles()
    {
        // Arrange
        List<ArticleDto> articles =
        [
            new() { Id = 1, ProductId = 1, Ean = "1234567890123" },
            new() { Id = 2, ProductId = 1, Ean = "1234567890124" }
        ];
        mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(articles);

        // Act
        ActionResult<Response<IReadOnlyList<ArticleDto>>> result = await controller.GetAll(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<ArticleDto>>? response = okResult!.Value as Response<IReadOnlyList<ArticleDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsOkWithArticle()
    {
        // Arrange
        const int articleId = 1;
        ArticleDto article = new() { Id = articleId, ProductId = 1, Ean = "1234567890123" };
        mockService.Setup(s => s.GetByIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ArticleDto>.Success(article));

        // Act
        ActionResult<Response<ArticleDto>> result = await controller.GetById(articleId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<ArticleDto>? response = okResult!.Value as Response<ArticleDto>;
        response!.Data!.Id.Should().Be(articleId);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int articleId = 999;
        mockService.Setup(s => s.GetByIdAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ArticleDto>.NotFound());

        // Act
        ActionResult<Response<ArticleDto>> result = await controller.GetById(articleId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByProductId Tests

    [Fact]
    public async Task GetByProductId_ValidProductId_ReturnsOkWithArticles()
    {
        // Arrange
        const int productId = 1;
        List<ArticleDto> articles = [new() { Id = 1, ProductId = productId, Ean = "1234567890123" }];
        mockService.Setup(s => s.GetByProductIdAsync(productId, It.IsAny<CancellationToken>())).ReturnsAsync(articles);

        // Act
        ActionResult<Response<IReadOnlyList<ArticleDto>>> result = await controller.GetByProductId(productId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<ArticleDto>>? response = okResult!.Value as Response<IReadOnlyList<ArticleDto>>;
        response!.Data.Should().HaveCount(1);
        response.Data![0].ProductId.Should().Be(productId);
    }

    #endregion

    #region GetActive Tests

    [Fact]
    public async Task GetActive_ReturnsOkWithActiveArticles()
    {
        // Arrange
        List<ArticleDto> articles = [new() { Id = 1, ProductId = 1, Ean = "1234567890123", IsActive = true }];
        mockService.Setup(s => s.GetActiveArticlesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(articles);

        // Act
        ActionResult<Response<IReadOnlyList<ArticleDto>>> result = await controller.GetActive(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<ArticleDto>>? response = okResult!.Value as Response<IReadOnlyList<ArticleDto>>;
        response!.Data.Should().HaveCount(1);
    }

    #endregion

    #region GetByEan Tests

    [Fact]
    public async Task GetByEan_ValidEan_ReturnsOkWithArticle()
    {
        // Arrange
        const string ean = "1234567890123";
        ArticleDto article = new() { Id = 1, Ean = ean };
        mockService.Setup(s => s.GetByEanAsync(ean, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ArticleDto>.Success(article));

        // Act
        ActionResult<Response<ArticleDto>> result = await controller.GetByEan(ean, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<ArticleDto>? response = okResult!.Value as Response<ArticleDto>;
        response!.Data!.Ean.Should().Be(ean);
    }

    [Fact]
    public async Task GetByEan_InvalidEan_ReturnsNotFound()
    {
        // Arrange
        const string ean = "9999999999999";
        mockService.Setup(s => s.GetByEanAsync(ean, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ArticleDto>.NotFound());

        // Act
        ActionResult<Response<ArticleDto>> result = await controller.GetByEan(ean, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        CreateArticleDto createDto = new() { ProductId = 1, Ean = "1234567890123", CurrentlyActive = true };
        ArticleDto created = new() { Id = 1, ProductId = 1, Ean = "1234567890123" };
        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<ArticleDto>> result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        CreatedAtActionResult? createdResult = result.Result as CreatedAtActionResult;
        Response<ArticleDto>? response = createdResult!.Value as Response<ArticleDto>;
        response!.Data!.Id.Should().Be(1);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int articleId = 1;
        UpdateArticleDto updateDto = new() { Ean = "1234567890123" };
        mockService.Setup(s => s.UpdateAsync(articleId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ArticleDto>.Success(new ArticleDto { Id = articleId }));

        // Act
        IActionResult result = await controller.Update(articleId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int articleId = 999;
        UpdateArticleDto updateDto = new() { Ean = "1234567890123" };
        mockService.Setup(s => s.UpdateAsync(articleId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<ArticleDto>.NotFound());

        // Act
        IActionResult result = await controller.Update(articleId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int articleId = 1;
        mockService.Setup(s => s.DeleteAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        IActionResult result = await controller.Delete(articleId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int articleId = 999;
        mockService.Setup(s => s.DeleteAsync(articleId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        IActionResult result = await controller.Delete(articleId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region CreateBatch Tests

    [Fact]
    public async Task CreateBatch_ValidDtos_ReturnsCreated()
    {
        // Arrange
        List<CreateArticleDto> createDtos = [new() { ProductId = 1, Ean = "1234567890123", CurrentlyActive = true }];
        List<ArticleDto> created = [new() { Id = 1, ProductId = 1, Ean = "1234567890123" }];
        mockService.Setup(s => s.CreateBatchAsync(createDtos, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<IReadOnlyList<ArticleDto>>> result = await controller.CreateBatch(createDtos, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(201);
    }

    #endregion
}
