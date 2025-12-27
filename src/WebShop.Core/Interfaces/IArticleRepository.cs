using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Core.Interfaces;

/// <summary>
/// Repository interface for article data access operations.
/// </summary>
public interface IArticleRepository : IRepository<Article>
{
    /// <summary>
    /// Gets all article variants for a specific product.
    /// </summary>
    /// <param name="productId">Product identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of articles (variants) for the specified product.</returns>
    Task<List<Article>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only articles that are currently active and available.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active articles.</returns>
    Task<List<Article>> GetActiveArticlesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets an article by EAN code.
    /// </summary>
    /// <param name="ean">European Article Number.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Article if found, otherwise null.</returns>
    Task<Article?> GetByEanAsync(string ean, CancellationToken cancellationToken = default);
}

