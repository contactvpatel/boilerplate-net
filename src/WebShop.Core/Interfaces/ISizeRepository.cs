using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Core.Interfaces;

/// <summary>
/// Repository interface for size data access operations.
/// </summary>
public interface ISizeRepository : IRepository<Size>
{
    /// <summary>
    /// Gets sizes filtered by gender classification and product category.
    /// </summary>
    /// <param name="gender">Gender classification (e.g., "Men", "Women", "Unisex").</param>
    /// <param name="category">Product category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sizes matching the gender and category criteria.</returns>
    Task<List<Size>> GetByGenderAndCategoryAsync(string gender, string category, CancellationToken cancellationToken = default);
}

