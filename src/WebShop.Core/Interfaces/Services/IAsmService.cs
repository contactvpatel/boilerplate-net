using WebShop.Core.Models;

namespace WebShop.Core.Interfaces.Services;

/// <summary>
/// Service interface for Application Security Management (ASM) operations.
/// </summary>
public interface IAsmService
{
    /// <summary>
    /// Gets application security information for a person based on their roles and positions.
    /// </summary>
    /// <param name="personId">Person identifier.</param>
    /// <param name="token">Authentication token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of application security information.</returns>
    Task<IReadOnlyList<AsmResponseModel>> GetApplicationSecurityAsync(
        string personId,
        string token,
        CancellationToken cancellationToken = default);
}
