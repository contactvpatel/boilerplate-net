using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Core.Interfaces;

/// <summary>
/// Repository interface for color data access operations.
/// </summary>
public interface IColorRepository : IRepository<Color>
{
    /// <summary>
    /// Gets a color by name.
    /// </summary>
    /// <param name="name">Color name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Color if found, otherwise null.</returns>
    Task<Color?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}

