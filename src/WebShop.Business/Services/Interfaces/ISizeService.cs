using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Service interface for size business operations.
/// </summary>
public interface ISizeService
{
    Task<SizeDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all available sizes in the system.
    /// </summary>
    Task<IReadOnlyList<SizeDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets sizes filtered by gender classification and product category.
    /// </summary>
    Task<IReadOnlyList<SizeDto>> GetByGenderAndCategoryAsync(string gender, string category, CancellationToken cancellationToken = default);

    Task<SizeDto> CreateAsync(CreateSizeDto createDto, CancellationToken cancellationToken = default);
    Task<SizeDto?> UpdateAsync(int id, UpdateSizeDto updateDto, CancellationToken cancellationToken = default);
    Task<SizeDto?> PatchAsync(int id, UpdateSizeDto patchDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple sizes in a batch operation.
    /// </summary>
    Task<IReadOnlyList<SizeDto>> CreateBatchAsync(IReadOnlyList<CreateSizeDto> createDtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple sizes in a batch operation.
    /// </summary>
    Task<IReadOnlyList<SizeDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateSizeDto UpdateDto)> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple sizes in a batch operation (soft delete).
    /// </summary>
    Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);
}

