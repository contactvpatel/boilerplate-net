using System.Security.Cryptography;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Business.Services;

/// <summary>
/// Business layer implementation of SSO service with caching support.
/// This service wraps the core SSO service and adds token validation caching to reduce external SSO service calls.
/// Uses HybridCache via ICacheService to prevent cache stampede and improve authentication response times.
/// </summary>
public class SsoService(
    Core.Interfaces.Services.ISsoService coreSsoService,
    ICacheService cacheService,
    ILogger<SsoService> logger) : Interfaces.ISsoService
{
    private readonly Core.Interfaces.Services.ISsoService _coreSsoService = coreSsoService ?? throw new ArgumentNullException(nameof(coreSsoService));
    private readonly ICacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly ILogger<SsoService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private const string TokenValidationCachePrefix = "token-validation-";

    /// <inheritdoc />
    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token validation failed: token is null or empty");
            return false;
        }

        // Create cache key from token hash (for security, don't cache the actual token)
        string cacheKey = $"{TokenValidationCachePrefix}{GetTokenHash(token)}";

        try
        {
            // Use HybridCache with GetOrCreateAsync for stampede protection
            // The factory will be called only on cache miss, and all concurrent requests will wait for the same result
            bool isValid = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async cancel =>
                {
                    // Validate token with core service
                    bool result = await _coreSsoService.ValidateTokenAsync(token, cancel).ConfigureAwait(false);
                    _logger.LogDebug("Token validation result: {Result}", result ? "valid" : "invalid");
                    return result;
                },
                expiration: TimeSpan.FromMinutes(5), // Cache valid tokens for 5 minutes
                localExpiration: TimeSpan.FromMinutes(5), // Local cache matches expiration
                cancellationToken: cancellationToken).ConfigureAwait(false);

            // For invalid tokens, we want shorter cache time to prevent abuse
            // After getting the result, if invalid, we'll cache it with a shorter expiration
            if (!isValid)
            {
                // Cache invalid tokens for only 30 seconds to prevent brute force attacks
                await _cacheService.SetAsync(
                    cacheKey,
                    false,
                    expiration: TimeSpan.FromSeconds(30),
                    localExpiration: TimeSpan.FromSeconds(30),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                _logger.LogWarning("Token validation failed: invalid token");
            }
            else
            {
                _logger.LogDebug("Token validation successful");
            }

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during token validation");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<DTOs.SsoAuthResponse?> RenewTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(accessToken, nameof(accessToken));
        ArgumentException.ThrowIfNullOrWhiteSpace(refreshToken, nameof(refreshToken));

        // Token renewal is a state-changing operation, so we don't cache it
        // Delegate to core service and map Core model to DTO
        Core.Models.SsoAuthResponse? coreResponse = await _coreSsoService.RenewTokenAsync(accessToken, refreshToken, cancellationToken).ConfigureAwait(false);
        return coreResponse?.Adapt<DTOs.SsoAuthResponse>();
    }

    /// <inheritdoc />
    public async Task<bool> LogoutAsync(string token, CancellationToken cancellationToken = default)
    {
        // Logout is a state-changing operation
        // Clear any cached validation results for this token
        if (!string.IsNullOrWhiteSpace(token))
        {
            string cacheKey = $"{TokenValidationCachePrefix}{GetTokenHash(token)}";
            await _cacheService.RemoveAsync(cacheKey, cancellationToken).ConfigureAwait(false);
        }

        // Delegate to core service
        return await _coreSsoService.LogoutAsync(token, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Generates a hash of the token for use as a cache key.
    /// </summary>
    private static string GetTokenHash(string token)
    {
        using SHA256 sha256 = System.Security.Cryptography.SHA256.Create();
        byte[] hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(token));
        return Convert.ToBase64String(hashBytes);
    }
}

