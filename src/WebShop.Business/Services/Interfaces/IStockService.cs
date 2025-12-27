using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Service interface for stock inventory business operations.
/// </summary>
public interface IStockService
{
    Task<StockDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all stock inventory records.
    /// </summary>
    Task<IReadOnlyList<StockDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<StockDto?> GetByArticleIdAsync(int articleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock records where inventory count is below the specified threshold.
    /// </summary>
    Task<IReadOnlyList<StockDto>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default);

    Task<StockDto> CreateAsync(CreateStockDto createDto, CancellationToken cancellationToken = default);
    Task<StockDto?> UpdateAsync(int id, UpdateStockDto updateDto, CancellationToken cancellationToken = default);
    Task<StockDto?> PatchAsync(int id, UpdateStockDto patchDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple stock entries in a batch operation.
    /// </summary>
    Task<IReadOnlyList<StockDto>> CreateBatchAsync(IReadOnlyList<CreateStockDto> createDtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple stock entries in a batch operation.
    /// </summary>
    Task<IReadOnlyList<StockDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateStockDto UpdateDto)> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple stock entries in a batch operation (soft delete).
    /// </summary>
    Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);
}

