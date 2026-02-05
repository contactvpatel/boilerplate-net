using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Business.DTOs;
using WebShop.Business.Services;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using Xunit;

namespace WebShop.Business.Tests.Services;

/// <summary>
/// Unit tests for StockService.
/// </summary>
[Trait("Category", "Unit")]
public class StockServiceTests
{
    private readonly Mock<IStockRepository> _mockRepository;
    private readonly Mock<ILogger<StockService>> _mockLogger;
    private readonly StockService _service;

    public StockServiceTests()
    {
        _mockRepository = new Mock<IStockRepository>();
        _mockLogger = new Mock<ILogger<StockService>>();
        _service = new StockService(_mockRepository.Object, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsStockDto()
    {
        // Arrange
        const int stockId = 1;
        Stock stock = new Stock
        {
            Id = stockId,
            ArticleId = 1,
            Count = 100
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stock);

        // Act
        StockDto? result = await _service.GetByIdAsync(stockId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(stockId);
        result.Count.Should().Be(100);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int stockId = 999;
        _mockRepository
            .Setup(r => r.GetByIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stock?)null);

        // Act
        StockDto? result = await _service.GetByIdAsync(stockId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllStocks()
    {
        // Arrange
        List<Stock> stocks = new List<Stock>
        {
            new() { Id = 1, ArticleId = 1, Count = 100 },
            new() { Id = 2, ArticleId = 2, Count = 50 }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(stocks);

        // Act
        IReadOnlyList<StockDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetByArticleIdAsync Tests

    [Fact]
    public async Task GetByArticleIdAsync_ValidArticleId_ReturnsStockDto()
    {
        // Arrange
        const int articleId = 1;
        Stock stock = new Stock
        {
            Id = 1,
            ArticleId = articleId,
            Count = 100
        };

        _mockRepository
            .Setup(r => r.GetByArticleIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stock);

        // Act
        StockDto? result = await _service.GetByArticleIdAsync(articleId);

        // Assert
        result.Should().NotBeNull();
        result!.ArticleId.Should().Be(articleId);
    }

    [Fact]
    public async Task GetByArticleIdAsync_InvalidArticleId_ReturnsNull()
    {
        // Arrange
        const int articleId = 999;

        _mockRepository
            .Setup(r => r.GetByArticleIdAsync(articleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stock?)null);

        // Act
        StockDto? result = await _service.GetByArticleIdAsync(articleId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetLowStockAsync Tests

    [Fact]
    public async Task GetLowStockAsync_ValidThreshold_ReturnsLowStockItems()
    {
        // Arrange
        const int threshold = 10;
        List<Stock> stocks = new List<Stock>
        {
            new() { Id = 1, ArticleId = 1, Count = 5 }, // Below threshold
            new() { Id = 2, ArticleId = 2, Count = 8 }  // Below threshold
        };

        _mockRepository
            .Setup(r => r.GetLowStockAsync(threshold, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stocks);

        // Act
        IReadOnlyList<StockDto> result = await _service.GetLowStockAsync(threshold);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesStock()
    {
        // Arrange
        CreateStockDto createDto = new CreateStockDto
        {
            ArticleId = 1,
            Count = 100
        };

        Stock stock = new Stock
        {
            Id = 1,
            ArticleId = 1,
            Count = 100
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stock s, CancellationToken cancellationToken) => s);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        StockDto result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(100);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateStockDto? createDto = null;

        // Act
        Func<Task> act = async () => await _service.CreateAsync(createDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidId_UpdatesStock()
    {
        // Arrange
        const int stockId = 1;
        UpdateStockDto updateDto = new UpdateStockDto
        {
            ArticleId = 1,
            Count = 200
        };

        Stock existingStock = new Stock
        {
            Id = stockId,
            ArticleId = 1,
            Count = 100
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStock);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        StockDto? result = await _service.UpdateAsync(stockId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Count.Should().Be(200);
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int stockId = 999;
        UpdateStockDto updateDto = new UpdateStockDto { Count = 200 };

        _mockRepository
            .Setup(r => r.GetByIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stock?)null);

        // Act
        StockDto? result = await _service.UpdateAsync(stockId, updateDto);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PatchAsync Tests

    [Fact]
    public async Task PatchAsync_ValidId_WithChanges_PatchesStock()
    {
        // Arrange
        const int stockId = 1;
        UpdateStockDto patchDto = new UpdateStockDto
        {
            Count = 150
        };

        Stock existingStock = new Stock
        {
            Id = stockId,
            ArticleId = 1,
            Count = 100
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStock);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        StockDto? result = await _service.PatchAsync(stockId, patchDto);

        // Assert
        result.Should().NotBeNull();
        result!.Count.Should().Be(150);
    }

    [Fact]
    public async Task PatchAsync_ValidId_NoChanges_ReturnsStockWithoutSaving()
    {
        // Arrange
        const int stockId = 1;
        UpdateStockDto patchDto = new UpdateStockDto
        {
            Count = 100 // Same as existing
        };

        Stock existingStock = new Stock
        {
            Id = stockId,
            ArticleId = 1,
            Count = 100
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStock);

        // Act
        StockDto? result = await _service.PatchAsync(stockId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesStock()
    {
        // Arrange
        const int stockId = 1;
        Stock stock = new Stock
        {
            Id = stockId,
            ArticleId = 1,
            Count = 100
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(stockId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(stock);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await _service.DeleteAsync(stockId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int stockId = 999;

        _mockRepository
            .Setup(r => r.ExistsAsync(stockId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.DeleteAsync(stockId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Batch Operations Tests

    [Fact]
    public async Task CreateBatchAsync_ValidDtos_CreatesStocks()
    {
        // Arrange
        List<CreateStockDto> createDtos = new List<CreateStockDto>
        {
            new() { ArticleId = 1, Count = 100 },
            new() { ArticleId = 2, Count = 50 }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Stock s, CancellationToken cancellationToken) => s);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<StockDto> result = await _service.CreateBatchAsync(createDtos);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateBatchAsync_ValidUpdates_UpdatesStocks()
    {
        // Arrange
        List<(int Id, UpdateStockDto UpdateDto)> updates = new List<(int, UpdateStockDto)>
        {
            (1, new UpdateStockDto { Count = 200 }),
            (2, new UpdateStockDto { Count = 150 })
        };

        List<Stock> stocks = new List<Stock>
        {
            new() { Id = 1, ArticleId = 1, Count = 100 },
            new() { Id = 2, ArticleId = 2, Count = 50 }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Stock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stocks);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<StockDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateBatchAsync_SomeStocksNotFound_SkipsMissingStocks()
    {
        // Arrange
        List<(int Id, UpdateStockDto UpdateDto)> updates = new List<(int, UpdateStockDto)>
        {
            (1, new UpdateStockDto { Count = 200 }),
            (2, new UpdateStockDto { Count = 150 }),
            (999, new UpdateStockDto { Count = 300 }) // Not found
        };

        List<Stock> stocks = new List<Stock>
        {
            new() { Id = 1, ArticleId = 1, Count = 100 },
            new() { Id = 2, ArticleId = 2, Count = 50 }
            // Stock 999 is missing
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Stock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stocks);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<StockDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Only 2 stocks updated, 1 skipped
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteBatchAsync_ValidIds_DeletesStocks()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2 };
        List<Stock> stocks = new List<Stock>
        {
            new() { Id = 1, ArticleId = 1, Count = 100 },
            new() { Id = 2, ArticleId = 2, Count = 50 }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Stock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stocks);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<int> result = await _service.DeleteBatchAsync(ids);

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
        CreateStockDto createDto = new CreateStockDto { ArticleId = 1, Count = 100 };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Stock()));

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.CreateAsync(createDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int stockId = 1;
        UpdateStockDto updateDto = new UpdateStockDto { Count = 200 };
        Stock existingStock = new Stock { Id = stockId, ArticleId = 1, Count = 100 };

        _mockRepository
            .Setup(r => r.GetByIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStock);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.UpdateAsync(stockId, updateDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PatchAsync_NoChanges_DoesNotCallUpdate()
    {
        // Arrange
        const int stockId = 1;
        Stock existingStock = new Stock
        {
            Id = stockId,
            ArticleId = 1,
            Count = 100
        };

        UpdateStockDto patchDto = new UpdateStockDto
        {
            ArticleId = 1, // Same value
            Count = 100 // Same value
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStock);

        // Act
        StockDto? result = await _service.PatchAsync(stockId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int stockId = 1;
        Stock existingStock = new Stock { Id = stockId, ArticleId = 1, Count = 100 };

        _mockRepository
            .Setup(r => r.ExistsAsync(stockId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(stockId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingStock);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteAsync(stockId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<CreateStockDto> createDtos = new List<CreateStockDto>
        {
            new() { ArticleId = 1, Count = 100 }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Stock()));

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.CreateBatchAsync(createDtos);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<(int Id, UpdateStockDto UpdateDto)> updates = new List<(int, UpdateStockDto)>
        {
            (1, new UpdateStockDto { Count = 200 })
        };

        List<Stock> stocks = new List<Stock>
        {
            new() { Id = 1, ArticleId = 1, Count = 100 }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Stock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stocks);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.UpdateBatchAsync(updates);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<int> ids = new List<int> { 1 };
        List<Stock> stocks = new List<Stock>
        {
            new() { Id = 1, ArticleId = 1, Count = 100 }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Stock, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(stocks);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Stock>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteBatchAsync(ids);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
