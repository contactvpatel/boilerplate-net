using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Core.Interfaces;

/// <summary>
/// Repository interface for stock data access operations.
/// </summary>
public interface IStockRepository : IRepository<Stock>
{
    /// <summary>
    /// Gets stock information for a specific article.
    /// </summary>
    /// <param name="articleId">Article identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stock entry if found, otherwise null.</returns>
    Task<Stock?> GetByArticleIdAsync(int articleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets stock entries where inventory count is below the specified threshold.
    /// Used for inventory alerts and reorder notifications.
    /// </summary>
    /// <param name="threshold">Minimum stock threshold value.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of stock entries with count less than the threshold.</returns>
    Task<List<Stock>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default);
}

