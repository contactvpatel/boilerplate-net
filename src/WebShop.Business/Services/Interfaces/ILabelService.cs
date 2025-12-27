using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Service interface for label (brand) business operations.
/// </summary>
public interface ILabelService
{
    Task<LabelDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all labels (brands) in the system.
    /// </summary>
    Task<IReadOnlyList<LabelDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<LabelDto?> GetBySlugNameAsync(string slugName, CancellationToken cancellationToken = default);

    Task<LabelDto> CreateAsync(CreateLabelDto createDto, CancellationToken cancellationToken = default);
    Task<LabelDto?> UpdateAsync(int id, UpdateLabelDto updateDto, CancellationToken cancellationToken = default);
    Task<LabelDto?> PatchAsync(int id, UpdateLabelDto patchDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple labels in a batch operation.
    /// </summary>
    Task<IReadOnlyList<LabelDto>> CreateBatchAsync(IReadOnlyList<CreateLabelDto> createDtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple labels in a batch operation.
    /// </summary>
    Task<IReadOnlyList<LabelDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateLabelDto UpdateDto)> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple labels in a batch operation (soft delete).
    /// </summary>
    Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);
}

