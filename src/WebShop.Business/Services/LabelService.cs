using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Business.Helpers;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for label business operations.
/// </summary>
public class LabelService(ILabelRepository labelRepository, IUnitOfWork unitOfWork, ILogger<LabelService> logger) : Interfaces.ILabelService
{
    public async Task<LabelDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Label? label = await labelRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return label?.Adapt<LabelDto>();
    }

    public async Task<IReadOnlyList<LabelDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Label> labels = await labelRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return labels.Adapt<IReadOnlyList<LabelDto>>();
    }

    public async Task<LabelDto?> GetBySlugNameAsync(string slugName, CancellationToken cancellationToken = default)
    {
        Label? label = await labelRepository.GetBySlugNameAsync(slugName, cancellationToken).ConfigureAwait(false);
        return label?.Adapt<LabelDto>();
    }

    public async Task<LabelDto> CreateAsync(CreateLabelDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        logger.LogInformation("Creating new label. Name: {Name}, SlugName: {SlugName}", createDto.Name, createDto.SlugName);
        Label label = createDto.Adapt<Label>();
        await labelRepository.AddAsync(label, cancellationToken).ConfigureAwait(false);
        await labelRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Label created successfully. LabelId: {LabelId}", label.Id);
        return label.Adapt<LabelDto>();
    }

    public async Task<LabelDto?> UpdateAsync(int id, UpdateLabelDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        logger.LogInformation("Updating label. LabelId: {LabelId}", id);
        Label? label = await labelRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (label == null)
        {
            return null;
        }

        updateDto.Adapt(label);
        await labelRepository.UpdateAsync(label, cancellationToken).ConfigureAwait(false);
        await labelRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Label updated successfully. LabelId: {LabelId}", id);
        return label.Adapt<LabelDto>();
    }

    public async Task<LabelDto?> PatchAsync(int id, UpdateLabelDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto);

        Label? label = await labelRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (label == null)
        {
            return null;
        }

        bool hasChanges = false;

        if (patchDto.Name != null && label.Name != patchDto.Name)
        {
            label.Name = patchDto.Name;
            hasChanges = true;
        }

        if (patchDto.SlugName != null && label.SlugName != patchDto.SlugName)
        {
            label.SlugName = patchDto.SlugName;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await labelRepository.UpdateAsync(label, cancellationToken).ConfigureAwait(false);
            await labelRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Label patched successfully. LabelId: {LabelId}", id);
        }

        return label.Adapt<LabelDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting label. LabelId: {LabelId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await labelRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        // Check if already soft-deleted
        Label? label = await labelRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (label == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            logger.LogInformation("Label already deleted. LabelId: {LabelId}", id);
            return true;
        }

        // Perform soft delete
        await labelRepository.DeleteAsync(label, cancellationToken).ConfigureAwait(false);
        await labelRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Label deleted successfully. LabelId: {LabelId}", id);
        return true;
    }

    public async Task<IReadOnlyList<LabelDto>> CreateBatchAsync(IReadOnlyList<CreateLabelDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos);

        if (createDtos.Count == 0)
        {
            return Array.Empty<LabelDto>();
        }

        logger.LogInformation("Creating {Count} labels in batch", createDtos.Count);
        List<Label> labels = createDtos.Select(dto => dto.Adapt<Label>()).ToList();

        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Label label in labels)
            {
                await labelRepository.AddAsync(label, ct).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch created {Count} labels successfully", labels.Count);
        return labels.Adapt<IReadOnlyList<LabelDto>>();
    }

    public async Task<IReadOnlyList<LabelDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateLabelDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        if (updates.Count == 0)
        {
            return Array.Empty<LabelDto>();
        }

        logger.LogInformation("Updating {Count} labels in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Label> labels = await labelRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Label> labelLookup = labels.ToDictionary(l => l.Id);

        List<LabelDto> updatedLabels = new(updates.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach ((int id, UpdateLabelDto updateDto) in updates)
            {
                if (labelLookup.TryGetValue(id, out Label? label))
                {
                    updateDto.Adapt(label);
                    await labelRepository.UpdateAsync(label, ct).ConfigureAwait(false);
                    updatedLabels.Add(label.Adapt<LabelDto>());
                }
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch updated {Count} labels successfully", updatedLabels.Count);
        return updatedLabels;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        logger.LogInformation("Deleting {Count} labels in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Label> labels = await labelRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(labels.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Label label in labels)
            {
                await labelRepository.DeleteAsync(label, ct).ConfigureAwait(false);
                deletedIds.Add(label.Id);
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch deleted {Count} labels successfully", deletedIds.Count);
        return deletedIds;
    }
}

