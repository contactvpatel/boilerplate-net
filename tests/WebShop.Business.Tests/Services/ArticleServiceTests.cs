using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Business.DTOs;
using WebShop.Business.Models;
using WebShop.Business.Services;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;
using Xunit;

namespace WebShop.Business.Tests.Services;

/// <summary>
/// Unit tests for ArticleService.
/// </summary>
[Trait("Category", "Unit")]
public class ArticleServiceTests
{
    private readonly Mock<IArticleRepository> mockRepository = new();
    private readonly Mock<IUnitOfWork> mockUnitOfWork = new();
    private readonly Mock<ILogger<ArticleService>> mockLogger = new();
    private readonly ArticleService service;

    public ArticleServiceTests()
    {
        service = new ArticleService(mockRepository.Object, mockUnitOfWork.Object, mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsArticleDto()
    {
        // Arrange
        const int articleId = 1;
        Article article = new Article
        {
            Id = articleId,
            ProductId = 1,
            Ean = "1234567890123",
            ColorId = 1
        };

        mockRepository
            .Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(article);

        // Act
        Result<ArticleDto> result = await service.GetByIdAsync(articleId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(articleId);
        result.Value.Ean.Should().Be("1234567890123");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int articleId = 999;
        mockRepository
            .Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Article?)null);

        // Act
        Result<ArticleDto> result = await service.GetByIdAsync(articleId);

        // Assert
        result.IsNotFound.Should().BeTrue();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllArticles()
    {
        // Arrange
        List<Article> articles = new List<Article>
        {
            new() { Id = 1, ProductId = 1, Ean = "EAN1" },
            new() { Id = 2, ProductId = 1, Ean = "EAN2" }
        };

        mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(articles);

        // Act
        IReadOnlyList<ArticleDto> result = await service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetByProductIdAsync Tests

    [Fact]
    public async Task GetByProductIdAsync_ValidProductId_ReturnsArticles()
    {
        // Arrange
        const int productId = 1;
        List<Article> articles = new List<Article>
        {
            new() { Id = 1, ProductId = productId, Ean = "EAN1" }
        };

        mockRepository
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(articles);

        // Act
        IReadOnlyList<ArticleDto> result = await service.GetByProductIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetActiveArticlesAsync Tests

    [Fact]
    public async Task GetActiveArticlesAsync_ReturnsActiveArticles()
    {
        // Arrange
        List<Article> articles = new List<Article>
        {
            new() { Id = 1, ProductId = 1, CurrentlyActive = true }
        };

        mockRepository
            .Setup(r => r.GetActiveArticlesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(articles);

        // Act
        IReadOnlyList<ArticleDto> result = await service.GetActiveArticlesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetByEanAsync Tests

    [Fact]
    public async Task GetByEanAsync_ValidEan_ReturnsArticleDto()
    {
        // Arrange
        const string ean = "1234567890123";
        Article article = new Article
        {
            Id = 1,
            ProductId = 1,
            Ean = ean
        };

        mockRepository
            .Setup(r => r.GetByEanAsync(ean, It.IsAny<CancellationToken>()))
            .ReturnsAsync(article);

        // Act
        Result<ArticleDto> result = await service.GetByEanAsync(ean);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Ean.Should().Be(ean);
    }

    [Fact]
    public async Task GetByEanAsync_InvalidEan_ReturnsNull()
    {
        // Arrange
        const string ean = "INVALID";

        mockRepository
            .Setup(r => r.GetByEanAsync(ean, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Article?)null);

        // Act
        Result<ArticleDto> result = await service.GetByEanAsync(ean);

        // Assert
        result.IsNotFound.Should().BeTrue();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesArticle()
    {
        // Arrange
        CreateArticleDto createDto = new CreateArticleDto
        {
            ProductId = 1,
            Ean = "1234567890123",
            ColorId = 1
        };

        Article article = new Article
        {
            Id = 1,
            ProductId = 1,
            Ean = "1234567890123",
            ColorId = 1
        };

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Article a, CancellationToken cancellationToken) => a);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        ArticleDto result = await service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Ean.Should().Be("1234567890123");
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateArticleDto? createDto = null;

        // Act
        Func<Task> act = async () => await service.CreateAsync(createDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidId_UpdatesArticle()
    {
        // Arrange
        const int articleId = 1;
        UpdateArticleDto updateDto = new UpdateArticleDto
        {
            Ean = "Updated EAN",
            Description = "Updated Description"
        };

        Article existingArticle = new Article
        {
            Id = articleId,
            ProductId = 1,
            Ean = "Original EAN"
        };

        mockRepository
            .Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArticle);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<ArticleDto> result = await service.UpdateAsync(articleId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Ean.Should().Be("Updated EAN");
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int articleId = 999;
        UpdateArticleDto updateDto = new UpdateArticleDto { Ean = "Updated EAN" };

        mockRepository
            .Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Article?)null);

        // Act
        Result<ArticleDto> result = await service.UpdateAsync(articleId, updateDto);

        // Assert
        result.IsNotFound.Should().BeTrue();
    }

    #endregion

    #region PatchAsync Tests

    [Fact]
    public async Task PatchAsync_ValidId_WithChanges_PatchesArticle()
    {
        // Arrange
        const int articleId = 1;
        UpdateArticleDto patchDto = new UpdateArticleDto
        {
            Ean = "Patched EAN"
        };

        Article existingArticle = new Article
        {
            Id = articleId,
            ProductId = 1,
            Ean = "Original EAN"
        };

        mockRepository
            .Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArticle);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<ArticleDto> result = await service.PatchAsync(articleId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Ean.Should().Be("Patched EAN");
    }

    [Fact]
    public async Task PatchAsync_ValidId_NoChanges_ReturnsArticleWithoutSaving()
    {
        // Arrange
        const int articleId = 1;
        UpdateArticleDto patchDto = new UpdateArticleDto
        {
            Ean = "Original EAN" // Same as existing
        };

        Article existingArticle = new Article
        {
            Id = articleId,
            ProductId = 1,
            Ean = "Original EAN"
        };

        mockRepository
            .Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArticle);

        // Act
        Result<ArticleDto> result = await service.PatchAsync(articleId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesArticle()
    {
        // Arrange
        const int articleId = 1;
        Article article = new Article
        {
            Id = articleId,
            ProductId = 1,
            Ean = "EAN123"
        };

        mockRepository
            .Setup(r => r.ExistsAsync(articleId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockRepository
            .Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(article);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await service.DeleteAsync(articleId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int articleId = 999;

        mockRepository
            .Setup(r => r.ExistsAsync(articleId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await service.DeleteAsync(articleId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Batch Operations Tests

    [Fact]
    public async Task CreateBatchAsync_ValidDtos_CreatesArticles()
    {
        // Arrange
        List<CreateArticleDto> createDtos = new List<CreateArticleDto>
        {
            new() { ProductId = 1, Ean = "EAN1" },
            new() { ProductId = 1, Ean = "EAN2" }
        };

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Article a, CancellationToken cancellationToken) => a);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<ArticleDto> result = await service.CreateBatchAsync(createDtos);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateBatchAsync_ValidUpdates_UpdatesArticles()
    {
        // Arrange
        List<(int Id, UpdateArticleDto UpdateDto)> updates = new List<(int, UpdateArticleDto)>
        {
            (1, new UpdateArticleDto { Ean = "Updated EAN1" }),
            (2, new UpdateArticleDto { Ean = "Updated EAN2" })
        };

        List<Article> articles = new List<Article>
        {
            new() { Id = 1, ProductId = 1, Ean = "Original EAN1" },
            new() { Id = 2, ProductId = 1, Ean = "Original EAN2" }
        };

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(articles);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IReadOnlyList<ArticleDto> result = await service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateBatchAsync_SomeArticlesNotFound_SkipsMissingArticles()
    {
        // Arrange
        List<(int Id, UpdateArticleDto UpdateDto)> updates = new List<(int, UpdateArticleDto)>
        {
            (1, new UpdateArticleDto { Ean = "Updated EAN1" }),
            (2, new UpdateArticleDto { Ean = "Updated EAN2" }),
            (999, new UpdateArticleDto { Ean = "Missing EAN" }) // Not found
        };

        List<Article> articles = new List<Article>
        {
            new() { Id = 1, ProductId = 1, Ean = "Original EAN1" },
            new() { Id = 2, ProductId = 1, Ean = "Original EAN2" }
            // Article 999 is missing
        };

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(articles);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IReadOnlyList<ArticleDto> result = await service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Only 2 articles updated, 1 skipped
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteBatchAsync_ValidIds_DeletesArticles()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2 };
        List<Article> articles = new List<Article>
        {
            new() { Id = 1, ProductId = 1, Ean = "EAN1" },
            new() { Id = 2, ProductId = 1, Ean = "EAN2" }
        };

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(articles);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IReadOnlyList<int> result = await service.DeleteBatchAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        CreateArticleDto createDto = new CreateArticleDto { ProductId = 1, Ean = "1234567890123" };

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Article()));

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.CreateAsync(createDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int articleId = 1;
        UpdateArticleDto updateDto = new UpdateArticleDto { Ean = "Updated EAN" };
        Article existingArticle = new Article { Id = articleId, ProductId = 1, Ean = "Original EAN" };

        mockRepository
            .Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArticle);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.UpdateAsync(articleId, updateDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PatchAsync_NoChanges_DoesNotCallUpdate()
    {
        // Arrange
        const int articleId = 1;
        Article existingArticle = new Article
        {
            Id = articleId,
            ProductId = 1,
            Ean = "EAN123",
            ColorId = 1,
            Size = 10,
            Description = "Description",
            CurrentlyActive = true
        };

        UpdateArticleDto patchDto = new UpdateArticleDto
        {
            Ean = "EAN123", // Same value
            ProductId = 1, // Same value
            ColorId = 1 // Same value
        };

        mockRepository
            .Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArticle);

        // Act
        Result<ArticleDto> result = await service.PatchAsync(articleId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()), Times.Never);
        mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int articleId = 1;
        Article existingArticle = new Article { Id = articleId, ProductId = 1, Ean = "EAN123" };

        mockRepository
            .Setup(r => r.ExistsAsync(articleId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockRepository
            .Setup(r => r.GetByIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingArticle);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.DeleteAsync(articleId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<CreateArticleDto> createDtos = new List<CreateArticleDto>
        {
            new() { ProductId = 1, Ean = "EAN1" }
        };

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.CreateBatchAsync(createDtos);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<(int Id, UpdateArticleDto UpdateDto)> updates = new List<(int, UpdateArticleDto)>
        {
            (1, new UpdateArticleDto { Ean = "Updated EAN" })
        };

        List<Article> articles = new List<Article>
        {
            new() { Id = 1, ProductId = 1, Ean = "Original EAN" }
        };

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(articles);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.UpdateBatchAsync(updates);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<int> ids = new List<int> { 1 };
        List<Article> articles = new List<Article>
        {
            new() { Id = 1, ProductId = 1, Ean = "EAN1" }
        };

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(articles);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Article>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.DeleteBatchAsync(ids);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
