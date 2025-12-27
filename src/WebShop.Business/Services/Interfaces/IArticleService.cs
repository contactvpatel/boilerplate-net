using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Service interface for article business operations.
/// </summary>
public interface IArticleService
{
    Task<ArticleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all articles (product variants) in the system.
    /// </summary>
    Task<IReadOnlyList<ArticleDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all article variants for a specific product.
    /// </summary>
    Task<IReadOnlyList<ArticleDto>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only active articles currently available for sale.
    /// </summary>
    Task<IReadOnlyList<ArticleDto>> GetActiveArticlesAsync(CancellationToken cancellationToken = default);

    Task<ArticleDto?> GetByEanAsync(string ean, CancellationToken cancellationToken = default);

    Task<ArticleDto> CreateAsync(CreateArticleDto createDto, CancellationToken cancellationToken = default);
    Task<ArticleDto?> UpdateAsync(int id, UpdateArticleDto updateDto, CancellationToken cancellationToken = default);
    Task<ArticleDto?> PatchAsync(int id, UpdateArticleDto patchDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple articles in a batch operation.
    /// </summary>
    Task<IReadOnlyList<ArticleDto>> CreateBatchAsync(IReadOnlyList<CreateArticleDto> createDtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple articles in a batch operation.
    /// </summary>
    Task<IReadOnlyList<ArticleDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateArticleDto UpdateDto)> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple articles in a batch operation (soft delete).
    /// </summary>
    Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);
}

