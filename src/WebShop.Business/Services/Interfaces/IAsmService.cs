using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Business layer interface for Application Security Management (ASM) operations.
/// Provides access to application security permissions and access rights.
/// </summary>
public interface IAsmService
{
    /// <summary>
    /// Gets application security information (permissions, access rights) for a person
    /// based on their roles and positions in the organization.
    /// </summary>
    Task<IReadOnlyList<AsmResponseDto>> GetApplicationSecurityAsync(
        string personId,
        string token,
        CancellationToken cancellationToken = default);
}

