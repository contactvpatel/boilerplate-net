using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Service interface for product business operations.
/// </summary>
public interface IProductService
{
    Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all products in the system.
    /// </summary>
    Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of products.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the paginated items and total count.</returns>
    Task<(IReadOnlyList<ProductDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<ProductDto> CreateAsync(CreateProductDto createDto, CancellationToken cancellationToken = default);
    Task<ProductDto?> UpdateAsync(int id, UpdateProductDto updateDto, CancellationToken cancellationToken = default);
    Task<ProductDto?> PatchAsync(int id, UpdateProductDto patchDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple products in a batch operation.
    /// </summary>
    Task<IReadOnlyList<ProductDto>> CreateBatchAsync(IReadOnlyList<CreateProductDto> createDtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple products in a batch operation.
    /// </summary>
    Task<IReadOnlyList<ProductDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateProductDto UpdateDto)> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple products in a batch operation (soft delete).
    /// </summary>
    Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products filtered by category name.
    /// </summary>
    Task<IReadOnlyList<ProductDto>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only active (non-deleted) products available for sale.
    /// </summary>
    Task<IReadOnlyList<ProductDto>> GetActiveProductsAsync(CancellationToken cancellationToken = default);
}

