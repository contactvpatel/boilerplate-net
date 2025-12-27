using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Service interface for color business operations.
/// </summary>
public interface IColorService
{
    Task<ColorDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available colors in the system.
    /// </summary>
    Task<IReadOnlyList<ColorDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<ColorDto?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<ColorDto> CreateAsync(CreateColorDto createDto, CancellationToken cancellationToken = default);
    Task<ColorDto?> UpdateAsync(int id, UpdateColorDto updateDto, CancellationToken cancellationToken = default);
    Task<ColorDto?> PatchAsync(int id, UpdateColorDto patchDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple colors in a batch operation.
    /// </summary>
    Task<IReadOnlyList<ColorDto>> CreateBatchAsync(IReadOnlyList<CreateColorDto> createDtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple colors in a batch operation.
    /// </summary>
    Task<IReadOnlyList<ColorDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateColorDto UpdateDto)> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple colors in a batch operation (soft delete).
    /// </summary>
    Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);
}

