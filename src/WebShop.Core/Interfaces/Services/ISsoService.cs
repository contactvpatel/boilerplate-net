using WebShop.Core.Models;

namespace WebShop.Core.Interfaces.Services;

/// <summary>
/// Service interface for Single Sign-On (SSO) token validation and authentication operations.
/// </summary>
public interface ISsoService
{
    /// <summary>
    /// Validates a JWT token with the SSO service.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the token is valid, false otherwise.</returns>
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>
    /// Renews an authentication token using a refresh token.
    /// </summary>
    /// <param name="accessToken">Current access token.</param>
    /// <param name="refreshToken">Refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New authentication information if successful, null otherwise.</returns>
    Task<SsoAuthResponse?> RenewTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Logs out the user by invalidating the token.
    /// </summary>
    /// <param name="token">Access token to invalidate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if logout was successful, false otherwise.</returns>
    Task<bool> LogoutAsync(string token, CancellationToken cancellationToken = default);
}
