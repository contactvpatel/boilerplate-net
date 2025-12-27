using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Core.Interfaces;

/// <summary>
/// Repository interface for label data access operations.
/// </summary>
public interface ILabelRepository : IRepository<Label>
{
    /// <summary>
    /// Gets a label by slug name.
    /// </summary>
    /// <param name="slugName">Slug name of the label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Label if found, otherwise null.</returns>
    Task<Label?> GetBySlugNameAsync(string slugName, CancellationToken cancellationToken = default);
}

