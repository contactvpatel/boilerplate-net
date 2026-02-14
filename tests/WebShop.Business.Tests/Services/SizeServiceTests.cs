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
/// Unit tests for SizeService.
/// </summary>
[Trait("Category", "Unit")]
public class SizeServiceTests
{
    private readonly Mock<ISizeRepository> mockRepository = new();
    private readonly Mock<IUnitOfWork> mockUnitOfWork = new();
    private readonly Mock<ILogger<SizeService>> mockLogger = new();
    private readonly SizeService service;

    public SizeServiceTests()
    {
        service = new SizeService(mockRepository.Object, mockUnitOfWork.Object, mockLogger.Object);
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

        mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(size);

        // Act
        Result<SizeDto> result = await service.GetByIdAsync(sizeId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(sizeId);
        result.Value.SizeLabel.Should().Be("M");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int sizeId = 999;
        mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Size?)null);

        // Act
        Result<SizeDto> result = await service.GetByIdAsync(sizeId);

        // Assert
        result.IsNotFound.Should().BeTrue();
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

        mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        // Act
        IReadOnlyList<SizeDto> result = await service.GetAllAsync();

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

        mockRepository
            .Setup(r => r.GetByGenderAndCategoryAsync(gender, category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        // Act
        IReadOnlyList<SizeDto> result = await service.GetByGenderAndCategoryAsync(gender, category);

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

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Size s, CancellationToken cancellationToken) => s);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        SizeDto result = await service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.SizeLabel.Should().Be("L");
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateSizeDto? createDto = null;

        // Act
        Func<Task> act = async () => await service.CreateAsync(createDto!);

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

        mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<SizeDto> result = await service.UpdateAsync(sizeId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SizeLabel.Should().Be("XL");
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int sizeId = 999;
        UpdateSizeDto updateDto = new UpdateSizeDto { SizeLabel = "XL" };

        mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Size?)null);

        // Act
        Result<SizeDto> result = await service.UpdateAsync(sizeId, updateDto);

        // Assert
        result.IsNotFound.Should().BeTrue();
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

        mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<SizeDto> result = await service.PatchAsync(sizeId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SizeLabel.Should().Be("XL");
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

        mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        // Act
        Result<SizeDto> result = await service.PatchAsync(sizeId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()), Times.Never);
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

        mockRepository
            .Setup(r => r.ExistsAsync(sizeId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(size);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await service.DeleteAsync(sizeId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int sizeId = 999;

        mockRepository
            .Setup(r => r.ExistsAsync(sizeId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await service.DeleteAsync(sizeId);

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

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Size s, CancellationToken cancellationToken) => s);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<SizeDto> result = await service.CreateBatchAsync(createDtos);

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

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IReadOnlyList<SizeDto> result = await service.UpdateBatchAsync(updates);

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

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
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
        CreateSizeDto createDto = new CreateSizeDto { SizeLabel = "L", Gender = "Male", Category = "Shirts" };

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Size()));

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
        const int sizeId = 1;
        UpdateSizeDto updateDto = new UpdateSizeDto { SizeLabel = "XL" };
        Size existingSize = new Size { Id = sizeId, SizeLabel = "L", Gender = "Male", Category = "Shirts" };

        mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.UpdateAsync(sizeId, updateDto);
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

        mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        // Act
        Result<SizeDto> result = await service.PatchAsync(sizeId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()), Times.Never);
        mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int sizeId = 1;
        Size existingSize = new Size { Id = sizeId, SizeLabel = "L", Gender = "Male", Category = "Shirts" };

        mockRepository
            .Setup(r => r.ExistsAsync(sizeId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockRepository
            .Setup(r => r.GetByIdAsync(sizeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSize);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.DeleteAsync(sizeId);
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

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.CreateBatchAsync(createDtos);
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

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
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
        List<Size> sizes = new List<Size>
        {
            new() { Id = 1, SizeLabel = "L", Gender = "Male", Category = "Shirts" }
        };

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(sizes);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Size>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.DeleteBatchAsync(ids);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
