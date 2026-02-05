using WebShop.Core.Models;

namespace WebShop.Core.Interfaces.Services;

/// <summary>
/// Contract for loading a user's application security from the central ASM system.
/// </summary>
public interface IAsmService
{
    /// <summary>
    /// Loads the application security data for the authenticated user (what they can access by role and position).
    /// </summary>
    /// <param name="personId">User/person identifier (e.g. from auth context), used for the ASM request and logging.</param>
    /// <param name="token">Authentication token for the ASM API.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The user's application security (roles, positions, and per-module access).</returns>
    Task<IReadOnlyList<AsmResponseModel>> GetApplicationSecurityAsync(
        string personId,
        string token,
        CancellationToken cancellationToken = default);
}
