using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for color business operations.
/// </summary>
public class ColorService(IColorRepository colorRepository, ILogger<ColorService> logger) : Interfaces.IColorService
{
    private readonly IColorRepository _colorRepository = colorRepository
        ?? throw new ArgumentNullException(nameof(colorRepository));
    private readonly ILogger<ColorService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<ColorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Color? color = await _colorRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return color?.Adapt<ColorDto>();
    }

    public async Task<IReadOnlyList<ColorDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Color> colors = await _colorRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return colors.Adapt<IReadOnlyList<ColorDto>>();
    }

    public async Task<ColorDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        Color? color = await _colorRepository.GetByNameAsync(name, cancellationToken).ConfigureAwait(false);
        return color?.Adapt<ColorDto>();
    }

    public async Task<ColorDto> CreateAsync(CreateColorDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        _logger.LogInformation("Creating new color. Name: {Name}, Rgb: {Rgb}", createDto.Name, createDto.Rgb);
        Color color = createDto.Adapt<Color>();
        await _colorRepository.AddAsync(color, cancellationToken).ConfigureAwait(false);
        await _colorRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Color created successfully. ColorId: {ColorId}", color.Id);
        return color.Adapt<ColorDto>();
    }

    public async Task<ColorDto?> UpdateAsync(int id, UpdateColorDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        _logger.LogInformation("Updating color. ColorId: {ColorId}", id);
        Color? color = await _colorRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (color == null)
        {
            _logger.LogWarning("Color not found for update. ColorId: {ColorId}", id);
            return null;
        }

        // TODO: Map update properties
        await _colorRepository.UpdateAsync(color, cancellationToken).ConfigureAwait(false);
        await _colorRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Color updated successfully. ColorId: {ColorId}", id);
        return color.Adapt<ColorDto>();
    }

    public async Task<ColorDto?> PatchAsync(int id, UpdateColorDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto, nameof(patchDto));

        Color? color = await _colorRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (color == null)
        {
            _logger.LogWarning("Color not found for patch. ColorId: {ColorId}", id);
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
            await _colorRepository.UpdateAsync(color, cancellationToken).ConfigureAwait(false);
            await _colorRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Color patched successfully. ColorId: {ColorId}", id);
        }

        return color.Adapt<ColorDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting color. ColorId: {ColorId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await _colorRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            // Never existed - return false (controller will return 404)
            _logger.LogWarning("Color not found for deletion. ColorId: {ColorId}", id);
            return false;
        }

        // Check if already soft-deleted
        Color? color = await _colorRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (color == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            _logger.LogInformation("Color already deleted. ColorId: {ColorId}", id);
            return true;
        }

        // Perform soft delete
        await _colorRepository.DeleteAsync(color, cancellationToken).ConfigureAwait(false);
        await _colorRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Color deleted successfully. ColorId: {ColorId}", id);
        return true;
    }

    public async Task<IReadOnlyList<ColorDto>> CreateBatchAsync(IReadOnlyList<CreateColorDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos, nameof(createDtos));

        if (createDtos.Count == 0)
        {
            return Array.Empty<ColorDto>();
        }

        _logger.LogInformation("Creating {Count} colors in batch", createDtos.Count);
        List<Color> colors = createDtos.Select(dto => dto.Adapt<Color>()).ToList();

        foreach (Color color in colors)
        {
            await _colorRepository.AddAsync(color, cancellationToken).ConfigureAwait(false);
        }

        await _colorRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch created {Count} colors successfully", colors.Count);
        return colors.Adapt<IReadOnlyList<ColorDto>>();
    }

    public async Task<IReadOnlyList<ColorDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateColorDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates, nameof(updates));

        if (updates.Count == 0)
        {
            return Array.Empty<ColorDto>();
        }

        _logger.LogInformation("Updating {Count} colors in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Color> colors = await _colorRepository.FindAsync(c => ids.Contains(c.Id), cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Color> colorLookup = colors.ToDictionary(c => c.Id);

        List<ColorDto> updatedColors = new(updates.Count);
        foreach ((int id, UpdateColorDto updateDto) in updates)
        {
            if (colorLookup.TryGetValue(id, out Color? color))
            {
                // TODO: Map update properties
                await _colorRepository.UpdateAsync(color, cancellationToken).ConfigureAwait(false);
                updatedColors.Add(color.Adapt<ColorDto>());
            }
        }

        await _colorRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch updated {Count} colors successfully", updatedColors.Count);
        return updatedColors;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        _logger.LogInformation("Deleting {Count} colors in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Color> colors = await _colorRepository.FindAsync(c => ids.Contains(c.Id), cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(colors.Count);
        foreach (Color color in colors)
        {
            await _colorRepository.DeleteAsync(color, cancellationToken).ConfigureAwait(false);
            deletedIds.Add(color.Id);
        }

        await _colorRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch deleted {Count} colors successfully", deletedIds.Count);
        return deletedIds;
    }
}

