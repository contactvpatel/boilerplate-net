using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Used to load a person's application security (what they can access by role and position) from the central ASM system.
/// </summary>
public interface IAsmService
{
    /// <summary>
    /// Returns which applications and actions a person can use, based on their roles and positions.
    /// </summary>
    /// <param name="personId">Person identifier, used for logging and diagnostics.</param>
    /// <param name="token">Authentication token for the ASM API.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of application security entries (role/position and per-application access).</returns>
    Task<IReadOnlyList<AsmResponseDto>> GetApplicationSecurityAsync(
        string personId,
        string token,
        CancellationToken cancellationToken = default);
}

