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
/// Unit tests for LabelService.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class LabelServiceTests
{
    private readonly Mock<ILabelRepository> mockRepository = new();
    private readonly Mock<IUnitOfWork> mockUnitOfWork = new();
    private readonly Mock<ILogger<LabelService>> mockLogger = new();
    private readonly LabelService service;

    public LabelServiceTests()
    {
        service = new LabelService(mockRepository.Object, mockUnitOfWork.Object, mockLogger.Object);
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

        mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        // Act
        Result<LabelDto> result = await service.GetByIdAsync(labelId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(labelId);
        result.Value.Name.Should().Be("Test Label");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int labelId = 999;
        mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label?)null);

        // Act
        Result<LabelDto> result = await service.GetByIdAsync(labelId);

        // Assert
        result.IsNotFound.Should().BeTrue();
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

        mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        // Act
        IReadOnlyList<LabelDto> result = await service.GetAllAsync();

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

        mockRepository
            .Setup(r => r.GetBySlugNameAsync(slugName, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        // Act
        Result<LabelDto> result = await service.GetBySlugNameAsync(slugName);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.SlugName.Should().Be(slugName);
    }

    [Fact]
    public async Task GetBySlugNameAsync_InvalidSlugName_ReturnsNull()
    {
        // Arrange
        const string slugName = "non-existent";

        mockRepository
            .Setup(r => r.GetBySlugNameAsync(slugName, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label?)null);

        // Act
        Result<LabelDto> result = await service.GetBySlugNameAsync(slugName);

        // Assert
        result.IsNotFound.Should().BeTrue();
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

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label l, CancellationToken cancellationToken) => l);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        LabelDto result = await service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Label");
        mockRepository.Verify(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateLabelDto? createDto = null;

        // Act
        Func<Task> act = async () => await service.CreateAsync(createDto!);

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

        mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<LabelDto> result = await service.UpdateAsync(labelId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Updated Label");
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int labelId = 999;
        UpdateLabelDto updateDto = new UpdateLabelDto { Name = "Updated Label" };

        mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label?)null);

        // Act
        Result<LabelDto> result = await service.UpdateAsync(labelId, updateDto);

        // Assert
        result.IsNotFound.Should().BeTrue();
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

        mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<LabelDto> result = await service.PatchAsync(labelId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Name.Should().Be("Patched Label");
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

        mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        // Act
        Result<LabelDto> result = await service.PatchAsync(labelId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
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

        mockRepository
            .Setup(r => r.ExistsAsync(labelId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(label);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await service.DeleteAsync(labelId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int labelId = 999;

        mockRepository
            .Setup(r => r.ExistsAsync(labelId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await service.DeleteAsync(labelId);

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

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Label l, CancellationToken cancellationToken) => l);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<LabelDto> result = await service.CreateBatchAsync(createDtos);

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

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IReadOnlyList<LabelDto> result = await service.UpdateBatchAsync(updates);

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

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
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
        CreateLabelDto createDto = new CreateLabelDto { Name = "New Label", SlugName = "new-label" };

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Label()));

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
        const int labelId = 1;
        UpdateLabelDto updateDto = new UpdateLabelDto { Name = "Updated Label" };
        Label existingLabel = new Label { Id = labelId, Name = "Original Label", SlugName = "original-label" };

        mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.UpdateAsync(labelId, updateDto);
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

        mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        // Act
        Result<LabelDto> result = await service.PatchAsync(labelId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()), Times.Never);
        mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int labelId = 1;
        Label existingLabel = new Label { Id = labelId, Name = "Label", SlugName = "label" };

        mockRepository
            .Setup(r => r.ExistsAsync(labelId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockRepository
            .Setup(r => r.GetByIdAsync(labelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingLabel);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.DeleteAsync(labelId);
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

        mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.CreateBatchAsync(createDtos);
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

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
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
        List<Label> labels = new List<Label>
        {
            new() { Id = 1, Name = "Label", SlugName = "label" }
        };

        mockRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(labels);

        mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Label>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.DeleteBatchAsync(ids);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
