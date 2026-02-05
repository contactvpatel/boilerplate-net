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
/// Unit tests for LabelService.
/// </summary>
[Trait("Category", "Unit")]
public class LabelServiceTests
{
    private readonly Mock<ILabelRepository> _mockRepository;
    private readonly Mock<ILogger<LabelService>> _mockLogger;
    private readonly LabelService _service;

    public LabelServiceTests()
    {
        _mockRepository = new Mock<ILabelRepository>();
        _mockLogger = new Mock<ILogger<LabelService>>();
        _service = new LabelService(_mockRepository.Object, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsLabelDto()
    {
        // Arrange
        const int labelId = 1;
        Label label = new Label
        {
            Id = labelId,
            Name = "Test Label",
            SlugName = "test-label"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        // Act
        LabelDto? result = await _service.GetByIdAsync(labelId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(labelId);
        result.Name.Should().Be("Test Label");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int labelId = 999;
        _mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label?)null);

        // Act
        LabelDto? result = await _service.GetByIdAsync(labelId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllLabels()
    {
        // Arrange
        List<Label> labels = new List<Label>
        {
            new() { Id = 1, Name = "Label 1", SlugName = "label-1" },
            new() { Id = 2, Name = "Label 2", SlugName = "label-2" }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        // Act
        IReadOnlyList<LabelDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region GetBySlugNameAsync Tests

    [Fact]
    public async Task GetBySlugNameAsync_ValidSlugName_ReturnsLabelDto()
    {
        // Arrange
        const string slugName = "test-label";
        Label label = new Label
        {
            Id = 1,
            Name = "Test Label",
            SlugName = slugName
        };

        _mockRepository
            .Setup(r => r.GetBySlugNameAsync(slugName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        // Act
        LabelDto? result = await _service.GetBySlugNameAsync(slugName);

        // Assert
        result.Should().NotBeNull();
        result!.SlugName.Should().Be(slugName);
    }

    [Fact]
    public async Task GetBySlugNameAsync_InvalidSlugName_ReturnsNull()
    {
        // Arrange
        const string slugName = "non-existent";

        _mockRepository
            .Setup(r => r.GetBySlugNameAsync(slugName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label?)null);

        // Act
        LabelDto? result = await _service.GetBySlugNameAsync(slugName);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesLabel()
    {
        // Arrange
        CreateLabelDto createDto = new CreateLabelDto
        {
            Name = "New Label",
            SlugName = "new-label"
        };

        Label label = new Label
        {
            Id = 1,
            Name = "New Label",
            SlugName = "new-label"
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label l, CancellationToken cancellationToken) => l);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        LabelDto result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Label");
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateLabelDto? createDto = null;

        // Act
        Func<Task> act = async () => await _service.CreateAsync(createDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidId_UpdatesLabel()
    {
        // Arrange
        const int labelId = 1;
        UpdateLabelDto updateDto = new UpdateLabelDto
        {
            Name = "Updated Label",
            SlugName = "updated-label"
        };

        Label existingLabel = new Label
        {
            Id = labelId,
            Name = "Original Label",
            SlugName = "original-label"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        LabelDto? result = await _service.UpdateAsync(labelId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Label");
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int labelId = 999;
        UpdateLabelDto updateDto = new UpdateLabelDto { Name = "Updated Label" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label?)null);

        // Act
        LabelDto? result = await _service.UpdateAsync(labelId, updateDto);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PatchAsync Tests

    [Fact]
    public async Task PatchAsync_ValidId_WithChanges_PatchesLabel()
    {
        // Arrange
        const int labelId = 1;
        UpdateLabelDto patchDto = new UpdateLabelDto
        {
            Name = "Patched Label"
        };

        Label existingLabel = new Label
        {
            Id = labelId,
            Name = "Original Label",
            SlugName = "original-label"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        LabelDto? result = await _service.PatchAsync(labelId, patchDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Patched Label");
    }

    [Fact]
    public async Task PatchAsync_ValidId_NoChanges_ReturnsLabelWithoutSaving()
    {
        // Arrange
        const int labelId = 1;
        UpdateLabelDto patchDto = new UpdateLabelDto
        {
            Name = "Original Label" // Same as existing
        };

        Label existingLabel = new Label
        {
            Id = labelId,
            Name = "Original Label",
            SlugName = "original-label"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        // Act
        LabelDto? result = await _service.PatchAsync(labelId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesLabel()
    {
        // Arrange
        const int labelId = 1;
        Label label = new Label
        {
            Id = labelId,
            Name = "Label to Delete",
            SlugName = "label-to-delete"
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(labelId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await _service.DeleteAsync(labelId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int labelId = 999;

        _mockRepository
            .Setup(r => r.ExistsAsync(labelId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.DeleteAsync(labelId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Batch Operations Tests

    [Fact]
    public async Task CreateBatchAsync_ValidDtos_CreatesLabels()
    {
        // Arrange
        List<CreateLabelDto> createDtos = new List<CreateLabelDto>
        {
            new() { Name = "Label 1", SlugName = "label-1" },
            new() { Name = "Label 2", SlugName = "label-2" }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label l, CancellationToken cancellationToken) => l);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<LabelDto> result = await _service.CreateBatchAsync(createDtos);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateBatchAsync_ValidUpdates_UpdatesLabels()
    {
        // Arrange
        List<(int Id, UpdateLabelDto UpdateDto)> updates = new List<(int, UpdateLabelDto)>
        {
            (1, new UpdateLabelDto { Name = "Updated Label 1" }),
            (2, new UpdateLabelDto { Name = "Updated Label 2" })
        };

        List<Label> labels = new List<Label>
        {
            new() { Id = 1, Name = "Original Label 1", SlugName = "original-1" },
            new() { Id = 2, Name = "Original Label 2", SlugName = "original-2" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<LabelDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteBatchAsync_ValidIds_DeletesLabels()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2 };
        List<Label> labels = new List<Label>
        {
            new() { Id = 1, Name = "Label 1", SlugName = "label-1" },
            new() { Id = 2, Name = "Label 2", SlugName = "label-2" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
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
        CreateLabelDto createDto = new CreateLabelDto { Name = "New Label", SlugName = "new-label" };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Label()));

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
        const int labelId = 1;
        UpdateLabelDto updateDto = new UpdateLabelDto { Name = "Updated Label" };
        Label existingLabel = new Label { Id = labelId, Name = "Original Label", SlugName = "original-label" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.UpdateAsync(labelId, updateDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PatchAsync_NoChanges_DoesNotCallUpdate()
    {
        // Arrange
        const int labelId = 1;
        Label existingLabel = new Label
        {
            Id = labelId,
            Name = "Original Label",
            SlugName = "original-label"
        };

        UpdateLabelDto patchDto = new UpdateLabelDto
        {
            Name = "Original Label", // Same value
            SlugName = "original-label" // Same value
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        // Act
        LabelDto? result = await _service.PatchAsync(labelId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int labelId = 1;
        Label existingLabel = new Label { Id = labelId, Name = "Label", SlugName = "label" };

        _mockRepository
            .Setup(r => r.ExistsAsync(labelId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteAsync(labelId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<CreateLabelDto> createDtos = new List<CreateLabelDto>
        {
            new() { Name = "Label", SlugName = "label" }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Label()));

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
        List<(int Id, UpdateLabelDto UpdateDto)> updates = new List<(int, UpdateLabelDto)>
        {
            (1, new UpdateLabelDto { Name = "Updated Label" })
        };

        List<Label> labels = new List<Label>
        {
            new() { Id = 1, Name = "Original Label", SlugName = "original-label" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
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
        List<Label> labels = new List<Label>
        {
            new() { Id = 1, Name = "Label", SlugName = "label" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Label, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
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
