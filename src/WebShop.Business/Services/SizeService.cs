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
/// Service implementation for size business operations.
/// </summary>
public class SizeService(ISizeRepository sizeRepository, IUnitOfWork unitOfWork, ILogger<SizeService> logger) : Interfaces.ISizeService
{
    public async Task<SizeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Size? size = await sizeRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return size?.Adapt<SizeDto>();
    }

    public async Task<IReadOnlyList<SizeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Size> sizes = await sizeRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return sizes.Adapt<IReadOnlyList<SizeDto>>();
    }

    public async Task<IReadOnlyList<SizeDto>> GetByGenderAndCategoryAsync(string gender, string category, CancellationToken cancellationToken = default)
    {
        List<Size> sizes = await sizeRepository.GetByGenderAndCategoryAsync(gender, category, cancellationToken).ConfigureAwait(false);
        return sizes.Adapt<IReadOnlyList<SizeDto>>();
    }

    public async Task<SizeDto> CreateAsync(CreateSizeDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        logger.LogInformation("Creating new size. SizeLabel: {SizeLabel}, Gender: {Gender}, Category: {Category}", createDto.SizeLabel, createDto.Gender, createDto.Category);
        Size size = createDto.Adapt<Size>();
        await sizeRepository.AddAsync(size, cancellationToken).ConfigureAwait(false);
        await sizeRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Size created successfully. SizeId: {SizeId}", size.Id);
        return size.Adapt<SizeDto>();
    }

    public async Task<SizeDto?> UpdateAsync(int id, UpdateSizeDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        logger.LogInformation("Updating size. SizeId: {SizeId}", id);
        Size? size = await sizeRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (size == null)
        {
            return null;
        }

        updateDto.Adapt(size);
        await sizeRepository.UpdateAsync(size, cancellationToken).ConfigureAwait(false);
        await sizeRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Size updated successfully. SizeId: {SizeId}", id);
        return size.Adapt<SizeDto>();
    }

    public async Task<SizeDto?> PatchAsync(int id, UpdateSizeDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto);

        Size? size = await sizeRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (size == null)
        {
            return null;
        }

        bool hasChanges = false;

        if (patchDto.Gender != null && size.Gender != patchDto.Gender)
        {
            size.Gender = patchDto.Gender;
            hasChanges = true;
        }

        if (patchDto.Category != null && size.Category != patchDto.Category)
        {
            size.Category = patchDto.Category;
            hasChanges = true;
        }

        if (patchDto.SizeLabel != null && size.SizeLabel != patchDto.SizeLabel)
        {
            size.SizeLabel = patchDto.SizeLabel;
            hasChanges = true;
        }

        if (patchDto.SizeUs != null && size.SizeUs != patchDto.SizeUs)
        {
            size.SizeUs = patchDto.SizeUs;
            hasChanges = true;
        }

        if (patchDto.SizeUk != null && size.SizeUk != patchDto.SizeUk)
        {
            size.SizeUk = patchDto.SizeUk;
            hasChanges = true;
        }

        if (patchDto.SizeEu != null && size.SizeEu != patchDto.SizeEu)
        {
            size.SizeEu = patchDto.SizeEu;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await sizeRepository.UpdateAsync(size, cancellationToken).ConfigureAwait(false);
            await sizeRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Size patched successfully. SizeId: {SizeId}", id);
        }

        return size.Adapt<SizeDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting size. SizeId: {SizeId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await sizeRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        // Check if already soft-deleted
        Size? size = await sizeRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (size == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            logger.LogInformation("Size already deleted. SizeId: {SizeId}", id);
            return true;
        }

        // Perform soft delete
        await sizeRepository.DeleteAsync(size, cancellationToken).ConfigureAwait(false);
        await sizeRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Size deleted successfully. SizeId: {SizeId}", id);
        return true;
    }

    public async Task<IReadOnlyList<SizeDto>> CreateBatchAsync(IReadOnlyList<CreateSizeDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos);

        if (createDtos.Count == 0)
        {
            return Array.Empty<SizeDto>();
        }

        logger.LogInformation("Creating {Count} sizes in batch", createDtos.Count);
        List<Size> sizes = createDtos.Select(dto => dto.Adapt<Size>()).ToList();

        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Size size in sizes)
            {
                await sizeRepository.AddAsync(size, ct).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch created {Count} sizes successfully", sizes.Count);
        return sizes.Adapt<IReadOnlyList<SizeDto>>();
    }

    public async Task<IReadOnlyList<SizeDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateSizeDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        if (updates.Count == 0)
        {
            return Array.Empty<SizeDto>();
        }

        logger.LogInformation("Updating {Count} sizes in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Size> sizes = await sizeRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Size> sizeLookup = sizes.ToDictionary(s => s.Id);

        List<SizeDto> updatedSizes = new(updates.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach ((int id, UpdateSizeDto updateDto) in updates)
            {
                if (sizeLookup.TryGetValue(id, out Size? size))
                {
                    updateDto.Adapt(size);
                    await sizeRepository.UpdateAsync(size, ct).ConfigureAwait(false);
                    updatedSizes.Add(size.Adapt<SizeDto>());
                }
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch updated {Count} sizes successfully", updatedSizes.Count);
        return updatedSizes;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        logger.LogInformation("Deleting {Count} sizes in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Size> sizes = await sizeRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(sizes.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Size size in sizes)
            {
                await sizeRepository.DeleteAsync(size, ct).ConfigureAwait(false);
                deletedIds.Add(size.Id);
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch deleted {Count} sizes successfully", deletedIds.Count);
        return deletedIds;
    }
}

