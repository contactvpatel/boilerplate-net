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
/// Unit tests for ColorService.
/// </summary>
[Trait("Category", "Unit")]
public class ColorServiceTests
{
    private readonly Mock<IColorRepository> _mockRepository;
    private readonly Mock<ILogger<ColorService>> _mockLogger;
    private readonly ColorService _service;

    public ColorServiceTests()
    {
        _mockRepository = new Mock<IColorRepository>();
        _mockLogger = new Mock<ILogger<ColorService>>();
        _service = new ColorService(_mockRepository.Object, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsColorDto()
    {
        // Arrange
        const int colorId = 1;
        Color color = new Color
        {
            Id = colorId,
            Name = "Red",
            Rgb = "#FF0000"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(colorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(color);

        // Act
        ColorDto? result = await _service.GetByIdAsync(colorId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(colorId);
        result.Name.Should().Be("Red");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int colorId = 999;
        _mockRepository
            .Setup(r => r.GetByIdAsync(colorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Color?)null);

        // Act
        ColorDto? result = await _service.GetByIdAsync(colorId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllColors()
    {
        // Arrange
        List<Color> colors = new List<Color>
        {
            new() { Id = 1, Name = "Red", Rgb = "#FF0000" },
            new() { Id = 2, Name = "Blue", Rgb = "#0000FF" }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(colors);

        // Act
        IReadOnlyList<ColorDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetByNameAsync Tests

    [Fact]
    public async Task GetByNameAsync_ValidName_ReturnsColorDto()
    {
        // Arrange
        const string name = "Red";
        Color color = new Color
        {
            Id = 1,
            Name = name,
            Rgb = "#FF0000"
        };

        _mockRepository
            .Setup(r => r.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(color);

        // Act
        ColorDto? result = await _service.GetByNameAsync(name);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be(name);
    }

    [Fact]
    public async Task GetByNameAsync_InvalidName_ReturnsNull()
    {
        // Arrange
        const string name = "NonExistent";

        _mockRepository
            .Setup(r => r.GetByNameAsync(name, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Color?)null);

        // Act
        ColorDto? result = await _service.GetByNameAsync(name);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesColor()
    {
        // Arrange
        CreateColorDto createDto = new CreateColorDto
        {
            Name = "Green",
            Rgb = "#00FF00"
        };

        Color color = new Color
        {
            Id = 1,
            Name = "Green",
            Rgb = "#00FF00"
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Color c, CancellationToken cancellationToken) => c);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        ColorDto result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Green");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateColorDto? createDto = null;

        // Act
        Func<Task> act = async () => await _service.CreateAsync(createDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidId_UpdatesColor()
    {
        // Arrange
        const int colorId = 1;
        UpdateColorDto updateDto = new UpdateColorDto
        {
            Name = "Updated Red",
            Rgb = "#FF0001"
        };

        Color existingColor = new Color
        {
            Id = colorId,
            Name = "Red",
            Rgb = "#FF0000"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(colorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingColor);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        ColorDto? result = await _service.UpdateAsync(colorId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Red");
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int colorId = 999;
        UpdateColorDto updateDto = new UpdateColorDto { Name = "Updated Red" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(colorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Color?)null);

        // Act
        ColorDto? result = await _service.UpdateAsync(colorId, updateDto);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PatchAsync Tests

    [Fact]
    public async Task PatchAsync_ValidId_WithChanges_PatchesColor()
    {
        // Arrange
        const int colorId = 1;
        UpdateColorDto patchDto = new UpdateColorDto
        {
            Name = "Patched Red"
        };

        Color existingColor = new Color
        {
            Id = colorId,
            Name = "Red",
            Rgb = "#FF0000"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(colorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingColor);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        ColorDto? result = await _service.PatchAsync(colorId, patchDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Patched Red");
    }

    [Fact]
    public async Task PatchAsync_ValidId_NoChanges_ReturnsColorWithoutSaving()
    {
        // Arrange
        const int colorId = 1;
        UpdateColorDto patchDto = new UpdateColorDto
        {
            Name = "Red" // Same as existing
        };

        Color existingColor = new Color
        {
            Id = colorId,
            Name = "Red",
            Rgb = "#FF0000"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(colorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingColor);

        // Act
        ColorDto? result = await _service.PatchAsync(colorId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesColor()
    {
        // Arrange
        const int colorId = 1;
        Color color = new Color
        {
            Id = colorId,
            Name = "Red",
            Rgb = "#FF0000"
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(colorId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(colorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(color);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await _service.DeleteAsync(colorId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int colorId = 999;

        _mockRepository
            .Setup(r => r.ExistsAsync(colorId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.DeleteAsync(colorId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Batch Operations Tests

    [Fact]
    public async Task CreateBatchAsync_ValidDtos_CreatesColors()
    {
        // Arrange
        List<CreateColorDto> createDtos = new List<CreateColorDto>
        {
            new() { Name = "Red", Rgb = "#FF0000" },
            new() { Name = "Blue", Rgb = "#0000FF" }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Color c, CancellationToken cancellationToken) => c);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<ColorDto> result = await _service.CreateBatchAsync(createDtos);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateBatchAsync_ValidUpdates_UpdatesColors()
    {
        // Arrange
        List<(int Id, UpdateColorDto UpdateDto)> updates = new List<(int, UpdateColorDto)>
        {
            (1, new UpdateColorDto { Name = "Updated Red" }),
            (2, new UpdateColorDto { Name = "Updated Blue" })
        };

        List<Color> colors = new List<Color>
        {
            new() { Id = 1, Name = "Red", Rgb = "#FF0000" },
            new() { Id = 2, Name = "Blue", Rgb = "#0000FF" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Color, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(colors);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<ColorDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteBatchAsync_ValidIds_DeletesColors()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2 };
        List<Color> colors = new List<Color>
        {
            new() { Id = 1, Name = "Red", Rgb = "#FF0000" },
            new() { Id = 2, Name = "Blue", Rgb = "#0000FF" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Color, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(colors);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
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
        CreateColorDto createDto = new CreateColorDto { Name = "Red", Rgb = "#FF0000" };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Color()));

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
        const int colorId = 1;
        UpdateColorDto updateDto = new UpdateColorDto { Name = "Updated Red" };
        Color existingColor = new Color { Id = colorId, Name = "Red", Rgb = "#FF0000" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(colorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingColor);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.UpdateAsync(colorId, updateDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PatchAsync_NoChanges_DoesNotCallUpdate()
    {
        // Arrange
        const int colorId = 1;
        Color existingColor = new Color
        {
            Id = colorId,
            Name = "Red",
            Rgb = "#FF0000"
        };

        UpdateColorDto patchDto = new UpdateColorDto
        {
            Name = "Red", // Same value
            Rgb = "#FF0000" // Same value
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(colorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingColor);

        // Act
        ColorDto? result = await _service.PatchAsync(colorId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int colorId = 1;
        Color existingColor = new Color { Id = colorId, Name = "Red", Rgb = "#FF0000" };

        _mockRepository
            .Setup(r => r.ExistsAsync(colorId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(colorId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingColor);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteAsync(colorId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<CreateColorDto> createDtos = new List<CreateColorDto>
        {
            new() { Name = "Red", Rgb = "#FF0000" }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Color()));

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
        List<(int Id, UpdateColorDto UpdateDto)> updates = new List<(int, UpdateColorDto)>
        {
            (1, new UpdateColorDto { Name = "Updated Red" })
        };

        List<Color> colors = new List<Color>
        {
            new() { Id = 1, Name = "Red", Rgb = "#FF0000" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Color, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(colors);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
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
        List<Color> colors = new List<Color>
        {
            new() { Id = 1, Name = "Red", Rgb = "#FF0000" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Color, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(colors);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Color>(), It.IsAny<CancellationToken>()))
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
