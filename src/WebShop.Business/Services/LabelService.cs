using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for label business operations.
/// </summary>
public class LabelService(ILabelRepository labelRepository, ILogger<LabelService> logger) : Interfaces.ILabelService
{
    private readonly ILabelRepository _labelRepository = labelRepository
        ?? throw new ArgumentNullException(nameof(labelRepository));
    private readonly ILogger<LabelService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<LabelDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Label? label = await _labelRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return label?.Adapt<LabelDto>();
    }

    public async Task<IReadOnlyList<LabelDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Label> labels = await _labelRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return labels.Adapt<IReadOnlyList<LabelDto>>();
    }

    public async Task<LabelDto?> GetBySlugNameAsync(string slugName, CancellationToken cancellationToken = default)
    {
        Label? label = await _labelRepository.GetBySlugNameAsync(slugName, cancellationToken).ConfigureAwait(false);
        return label?.Adapt<LabelDto>();
    }

    public async Task<LabelDto> CreateAsync(CreateLabelDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        _logger.LogInformation("Creating new label. Name: {Name}, SlugName: {SlugName}", createDto.Name, createDto.SlugName);
        Label label = createDto.Adapt<Label>();
        await _labelRepository.AddAsync(label, cancellationToken).ConfigureAwait(false);
        await _labelRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Label created successfully. LabelId: {LabelId}", label.Id);
        return label.Adapt<LabelDto>();
    }

    public async Task<LabelDto?> UpdateAsync(int id, UpdateLabelDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        _logger.LogInformation("Updating label. LabelId: {LabelId}", id);
        Label? label = await _labelRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (label == null)
        {
            _logger.LogWarning("Label not found for update. LabelId: {LabelId}", id);
            return null;
        }

        // TODO: Map update properties
        await _labelRepository.UpdateAsync(label, cancellationToken).ConfigureAwait(false);
        await _labelRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Label updated successfully. LabelId: {LabelId}", id);
        return label.Adapt<LabelDto>();
    }

    public async Task<LabelDto?> PatchAsync(int id, UpdateLabelDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto, nameof(patchDto));

        Label? label = await _labelRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (label == null)
        {
            _logger.LogWarning("Label not found for patch. LabelId: {LabelId}", id);
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
            await _labelRepository.UpdateAsync(label, cancellationToken).ConfigureAwait(false);
            await _labelRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Label patched successfully. LabelId: {LabelId}", id);
        }

        return label.Adapt<LabelDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting label. LabelId: {LabelId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await _labelRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            // Never existed - return false (controller will return 404)
            _logger.LogWarning("Label not found for deletion. LabelId: {LabelId}", id);
            return false;
        }

        // Check if already soft-deleted
        Label? label = await _labelRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (label == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            _logger.LogInformation("Label already deleted. LabelId: {LabelId}", id);
            return true;
        }

        // Perform soft delete
        await _labelRepository.DeleteAsync(label, cancellationToken).ConfigureAwait(false);
        await _labelRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Label deleted successfully. LabelId: {LabelId}", id);
        return true;
    }

    public async Task<IReadOnlyList<LabelDto>> CreateBatchAsync(IReadOnlyList<CreateLabelDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos, nameof(createDtos));

        if (createDtos.Count == 0)
        {
            return Array.Empty<LabelDto>();
        }

        _logger.LogInformation("Creating {Count} labels in batch", createDtos.Count);
        List<Label> labels = createDtos.Select(dto => dto.Adapt<Label>()).ToList();

        foreach (Label label in labels)
        {
            await _labelRepository.AddAsync(label, cancellationToken).ConfigureAwait(false);
        }

        await _labelRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch created {Count} labels successfully", labels.Count);
        return labels.Adapt<IReadOnlyList<LabelDto>>();
    }

    public async Task<IReadOnlyList<LabelDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateLabelDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates, nameof(updates));

        if (updates.Count == 0)
        {
            return Array.Empty<LabelDto>();
        }

        _logger.LogInformation("Updating {Count} labels in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Label> labels = await _labelRepository.FindAsync(l => ids.Contains(l.Id), cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Label> labelLookup = labels.ToDictionary(l => l.Id);

        List<LabelDto> updatedLabels = new(updates.Count);
        foreach ((int id, UpdateLabelDto updateDto) in updates)
        {
            if (labelLookup.TryGetValue(id, out Label? label))
            {
                // TODO: Map update properties
                await _labelRepository.UpdateAsync(label, cancellationToken).ConfigureAwait(false);
                updatedLabels.Add(label.Adapt<LabelDto>());
            }
        }

        await _labelRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch updated {Count} labels successfully", updatedLabels.Count);
        return updatedLabels;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        _logger.LogInformation("Deleting {Count} labels in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Label> labels = await _labelRepository.FindAsync(l => ids.Contains(l.Id), cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(labels.Count);
        foreach (Label label in labels)
        {
            await _labelRepository.DeleteAsync(label, cancellationToken).ConfigureAwait(false);
            deletedIds.Add(label.Id);
        }

        await _labelRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch deleted {Count} labels successfully", deletedIds.Count);
        return deletedIds;
    }
}

