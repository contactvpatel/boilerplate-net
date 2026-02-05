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
/// Unit tests for SizeService.
/// </summary>
[Trait("Category", "Unit")]
public class SizeServiceTests
{
    private readonly Mock<ISizeRepository> _mockRepository;
    private readonly Mock<ILogger<SizeService>> _mockLogger;
    private readonly SizeService _service;

    public SizeServiceTests()
    {
        _mockRepository = new Mock<ISizeRepository>();
        _mockLogger = new Mock<ILogger<SizeService>>();
        _service = new SizeService(_mockRepository.Object, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsSizeDto()
    {
        // Arrange
        const int sizeId = 1;
        Size size = new Size
        {
            Id = sizeId,
            SizeLabel = "M",
            Gender = "Male",
            Category = "Shirts"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(size);

        // Act
        SizeDto? result = await _service.GetByIdAsync(sizeId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(sizeId);
        result.SizeLabel.Should().Be("M");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int sizeId = 999;
        _mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Size?)null);

        // Act
        SizeDto? result = await _service.GetByIdAsync(sizeId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllSizes()
    {
        // Arrange
        List<Size> sizes = new List<Size>
        {
            new() { Id = 1, SizeLabel = "S", Gender = "Male", Category = "Shirts" },
            new() { Id = 2, SizeLabel = "M", Gender = "Male", Category = "Shirts" }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        // Act
        IReadOnlyList<SizeDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetByGenderAndCategoryAsync Tests

    [Fact]
    public async Task GetByGenderAndCategoryAsync_ValidParams_ReturnsSizes()
    {
        // Arrange
        const string gender = "Male";
        const string category = "Shirts";
        List<Size> sizes = new List<Size>
        {
            new() { Id = 1, SizeLabel = "S", Gender = gender, Category = category }
        };

        _mockRepository
            .Setup(r => r.GetByGenderAndCategoryAsync(gender, category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        // Act
        IReadOnlyList<SizeDto> result = await _service.GetByGenderAndCategoryAsync(gender, category);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesSize()
    {
        // Arrange
        CreateSizeDto createDto = new CreateSizeDto
        {
            SizeLabel = "L",
            Gender = "Male",
            Category = "Shirts"
        };

        Size size = new Size
        {
            Id = 1,
            SizeLabel = "L",
            Gender = "Male",
            Category = "Shirts"
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Size s, CancellationToken cancellationToken) => s);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        SizeDto result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.SizeLabel.Should().Be("L");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateSizeDto? createDto = null;

        // Act
        Func<Task> act = async () => await _service.CreateAsync(createDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidId_UpdatesSize()
    {
        // Arrange
        const int sizeId = 1;
        UpdateSizeDto updateDto = new UpdateSizeDto
        {
            SizeLabel = "XL",
            Gender = "Male",
            Category = "Shirts"
        };

        Size existingSize = new Size
        {
            Id = sizeId,
            SizeLabel = "L",
            Gender = "Male",
            Category = "Shirts"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        SizeDto? result = await _service.UpdateAsync(sizeId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.SizeLabel.Should().Be("XL");
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int sizeId = 999;
        UpdateSizeDto updateDto = new UpdateSizeDto { SizeLabel = "XL" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Size?)null);

        // Act
        SizeDto? result = await _service.UpdateAsync(sizeId, updateDto);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PatchAsync Tests

    [Fact]
    public async Task PatchAsync_ValidId_WithChanges_PatchesSize()
    {
        // Arrange
        const int sizeId = 1;
        UpdateSizeDto patchDto = new UpdateSizeDto
        {
            SizeLabel = "XL"
        };

        Size existingSize = new Size
        {
            Id = sizeId,
            SizeLabel = "L",
            Gender = "Male",
            Category = "Shirts"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        SizeDto? result = await _service.PatchAsync(sizeId, patchDto);

        // Assert
        result.Should().NotBeNull();
        result!.SizeLabel.Should().Be("XL");
    }

    [Fact]
    public async Task PatchAsync_ValidId_NoChanges_ReturnsSizeWithoutSaving()
    {
        // Arrange
        const int sizeId = 1;
        UpdateSizeDto patchDto = new UpdateSizeDto
        {
            SizeLabel = "L" // Same as existing
        };

        Size existingSize = new Size
        {
            Id = sizeId,
            SizeLabel = "L",
            Gender = "Male",
            Category = "Shirts"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        // Act
        SizeDto? result = await _service.PatchAsync(sizeId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesSize()
    {
        // Arrange
        const int sizeId = 1;
        Size size = new Size
        {
            Id = sizeId,
            SizeLabel = "L",
            Gender = "Male",
            Category = "Shirts"
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(sizeId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(size);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await _service.DeleteAsync(sizeId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int sizeId = 999;

        _mockRepository
            .Setup(r => r.ExistsAsync(sizeId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.DeleteAsync(sizeId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Batch Operations Tests

    [Fact]
    public async Task CreateBatchAsync_ValidDtos_CreatesSizes()
    {
        // Arrange
        List<CreateSizeDto> createDtos = new List<CreateSizeDto>
        {
            new() { SizeLabel = "S", Gender = "Male", Category = "Shirts" },
            new() { SizeLabel = "M", Gender = "Male", Category = "Shirts" }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Size s, CancellationToken cancellationToken) => s);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<SizeDto> result = await _service.CreateBatchAsync(createDtos);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateBatchAsync_ValidUpdates_UpdatesSizes()
    {
        // Arrange
        List<(int Id, UpdateSizeDto UpdateDto)> updates = new List<(int, UpdateSizeDto)>
        {
            (1, new UpdateSizeDto { SizeLabel = "XL" }),
            (2, new UpdateSizeDto { SizeLabel = "XXL" })
        };

        List<Size> sizes = new List<Size>
        {
            new() { Id = 1, SizeLabel = "L", Gender = "Male", Category = "Shirts" },
            new() { Id = 2, SizeLabel = "XL", Gender = "Male", Category = "Shirts" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Size, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<SizeDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteBatchAsync_ValidIds_DeletesSizes()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2 };
        List<Size> sizes = new List<Size>
        {
            new() { Id = 1, SizeLabel = "S", Gender = "Male", Category = "Shirts" },
            new() { Id = 2, SizeLabel = "M", Gender = "Male", Category = "Shirts" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Size, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
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
        CreateSizeDto createDto = new CreateSizeDto { SizeLabel = "L", Gender = "Male", Category = "Shirts" };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Size()));

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
        const int sizeId = 1;
        UpdateSizeDto updateDto = new UpdateSizeDto { SizeLabel = "XL" };
        Size existingSize = new Size { Id = sizeId, SizeLabel = "L", Gender = "Male", Category = "Shirts" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.UpdateAsync(sizeId, updateDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PatchAsync_NoChanges_DoesNotCallUpdate()
    {
        // Arrange
        const int sizeId = 1;
        Size existingSize = new Size
        {
            Id = sizeId,
            SizeLabel = "L",
            Gender = "Male",
            Category = "Shirts"
        };

        UpdateSizeDto patchDto = new UpdateSizeDto
        {
            SizeLabel = "L", // Same value
            Gender = "Male", // Same value
            Category = "Shirts" // Same value
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        // Act
        SizeDto? result = await _service.PatchAsync(sizeId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int sizeId = 1;
        Size existingSize = new Size { Id = sizeId, SizeLabel = "L", Gender = "Male", Category = "Shirts" };

        _mockRepository
            .Setup(r => r.ExistsAsync(sizeId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteAsync(sizeId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<CreateSizeDto> createDtos = new List<CreateSizeDto>
        {
            new() { SizeLabel = "L", Gender = "Male", Category = "Shirts" }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Size()));

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
        List<(int Id, UpdateSizeDto UpdateDto)> updates = new List<(int, UpdateSizeDto)>
        {
            (1, new UpdateSizeDto { SizeLabel = "XL" })
        };

        List<Size> sizes = new List<Size>
        {
            new() { Id = 1, SizeLabel = "L", Gender = "Male", Category = "Shirts" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Size, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
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
        List<Size> sizes = new List<Size>
        {
            new() { Id = 1, SizeLabel = "L", Gender = "Male", Category = "Shirts" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Size, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
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
