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
/// Service implementation for color business operations.
/// </summary>
public class ColorService(IColorRepository colorRepository, IUnitOfWork unitOfWork, ILogger<ColorService> logger) : Interfaces.IColorService
{
    public async Task<ColorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Color? color = await colorRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return color?.Adapt<ColorDto>();
    }

    public async Task<IReadOnlyList<ColorDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Color> colors = await colorRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return colors.Adapt<IReadOnlyList<ColorDto>>();
    }

    public async Task<ColorDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        Color? color = await colorRepository.GetByNameAsync(name, cancellationToken).ConfigureAwait(false);
        return color?.Adapt<ColorDto>();
    }

    public async Task<ColorDto> CreateAsync(CreateColorDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        logger.LogInformation("Creating new color. Name: {Name}, Rgb: {Rgb}", createDto.Name, createDto.Rgb);
        Color color = createDto.Adapt<Color>();
        await colorRepository.AddAsync(color, cancellationToken).ConfigureAwait(false);
        await colorRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Color created successfully. ColorId: {ColorId}", color.Id);
        return color.Adapt<ColorDto>();
    }

    public async Task<ColorDto?> UpdateAsync(int id, UpdateColorDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        logger.LogInformation("Updating color. ColorId: {ColorId}", id);
        Color? color = await colorRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (color == null)
        {
            return null;
        }

        updateDto.Adapt(color);
        await colorRepository.UpdateAsync(color, cancellationToken).ConfigureAwait(false);
        await colorRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Color updated successfully. ColorId: {ColorId}", id);
        return color.Adapt<ColorDto>();
    }

    public async Task<ColorDto?> PatchAsync(int id, UpdateColorDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto);

        Color? color = await colorRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (color == null)
        {
            return null;
        }

        bool hasChanges = false;

        if (patchDto.Name != null && color.Name != patchDto.Name)
        {
            color.Name = patchDto.Name;
            hasChanges = true;
        }

        if (patchDto.Rgb != null && color.Rgb != patchDto.Rgb)
        {
            color.Rgb = patchDto.Rgb;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await colorRepository.UpdateAsync(color, cancellationToken).ConfigureAwait(false);
            await colorRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Color patched successfully. ColorId: {ColorId}", id);
        }

        return color.Adapt<ColorDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting color. ColorId: {ColorId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await colorRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        // Check if already soft-deleted
        Color? color = await colorRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (color == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            logger.LogInformation("Color already deleted. ColorId: {ColorId}", id);
            return true;
        }

        // Perform soft delete
        await colorRepository.DeleteAsync(color, cancellationToken).ConfigureAwait(false);
        await colorRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Color deleted successfully. ColorId: {ColorId}", id);
        return true;
    }

    public async Task<IReadOnlyList<ColorDto>> CreateBatchAsync(IReadOnlyList<CreateColorDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos);

        if (createDtos.Count == 0)
        {
            return Array.Empty<ColorDto>();
        }

        logger.LogInformation("Creating {Count} colors in batch", createDtos.Count);
        List<Color> colors = createDtos.Select(dto => dto.Adapt<Color>()).ToList();

        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Color color in colors)
            {
                await colorRepository.AddAsync(color, ct).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch created {Count} colors successfully", colors.Count);
        return colors.Adapt<IReadOnlyList<ColorDto>>();
    }

    public async Task<IReadOnlyList<ColorDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateColorDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        if (updates.Count == 0)
        {
            return Array.Empty<ColorDto>();
        }

        logger.LogInformation("Updating {Count} colors in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Color> colors = await colorRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Color> colorLookup = colors.ToDictionary(c => c.Id);

        List<ColorDto> updatedColors = new(updates.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach ((int id, UpdateColorDto updateDto) in updates)
            {
                if (colorLookup.TryGetValue(id, out Color? color))
                {
                    updateDto.Adapt(color);
                    await colorRepository.UpdateAsync(color, ct).ConfigureAwait(false);
                    updatedColors.Add(color.Adapt<ColorDto>());
                }
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch updated {Count} colors successfully", updatedColors.Count);
        return updatedColors;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        logger.LogInformation("Deleting {Count} colors in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Color> colors = await colorRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(colors.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Color color in colors)
            {
                await colorRepository.DeleteAsync(color, ct).ConfigureAwait(false);
                deletedIds.Add(color.Id);
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch deleted {Count} colors successfully", deletedIds.Count);
        return deletedIds;
    }
}

