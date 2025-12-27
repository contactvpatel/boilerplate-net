using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Core.Interfaces;

/// <summary>
/// Repository interface for product data access operations.
/// </summary>
public interface IProductRepository : IRepository<Product>
{
    /// <summary>
    /// Gets products filtered by category name.
    /// </summary>
    /// <param name="category">Product category name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of products in the specified category.</returns>
    Task<List<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only products that are currently active and available for sale.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active products.</returns>
    Task<List<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets products associated with a specific label (brand).
    /// </summary>
    /// <param name="labelId">Label (brand) identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of products for the specified label.</returns>
    Task<List<Product>> GetByLabelIdAsync(int labelId, CancellationToken cancellationToken = default);
}

