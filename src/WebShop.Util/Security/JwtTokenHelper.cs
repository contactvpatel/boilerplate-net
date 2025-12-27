using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;

namespace WebShop.Util.Security;

/// <summary>
/// Helper class for JWT token operations and claim extraction.
/// </summary>
public static class JwtTokenHelper
{
    /// <summary>
    /// Standard claim type for user ID (Principal ID).
    /// </summary>
    public const string ClaimTypeUserId = "pid";

    /// <summary>
    /// Standard claim type for token expiration.
    /// </summary>
    public const string ClaimTypeExpiration = "exp";

    /// <summary>
    /// Cache key prefix for JWT token validation cache entries.
    /// </summary>
    public const string JwtTokenCacheKeyPrefix = "jwt_token:";

    /// <summary>
    /// Extracts the JWT security token from a token string.
    /// </summary>
    /// <param name="token">The JWT token string.</param>
    /// <returns>The JWT security token, or null if parsing fails.</returns>
    public static JwtSecurityToken? ParseToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.ReadToken(token) as JwtSecurityToken;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Extracts a claim value from a JWT token.
    /// </summary>
    /// <param name="token">The JWT security token.</param>
    /// <param name="claimType">The type of claim to extract.</param>
    /// <returns>The claim value, or null if not found.</returns>
    public static string? GetClaimValue(JwtSecurityToken? token, string claimType)
    {
        if (token == null || string.IsNullOrWhiteSpace(claimType))
        {
            return null;
        }

        return token.Claims.FirstOrDefault(c => c.Type.Equals(claimType, StringComparison.OrdinalIgnoreCase))?.Value;
    }

    /// <summary>
    /// Extracts the user ID from a JWT token.
    /// </summary>
    /// <param name="token">The JWT security token.</param>
    /// <returns>The user ID, or null if not found.</returns>
    public static string? GetUserId(JwtSecurityToken? token)
    {
        return GetClaimValue(token, ClaimTypeUserId);
    }

    /// <summary>
    /// Gets the token expiration time as a local DateTimeOffset.
    /// </summary>
    /// <param name="token">The JWT security token.</param>
    /// <returns>The expiration time as a local DateTimeOffset, or null if not found.</returns>
    public static DateTimeOffset? GetTokenExpiration(JwtSecurityToken? token)
    {
        string? expClaim = GetClaimValue(token, ClaimTypeExpiration);
        if (string.IsNullOrWhiteSpace(expClaim) || !long.TryParse(expClaim, out long expSeconds))
        {
            return null;
        }

        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(expSeconds).ToLocalTime();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Checks if a token is expired.
    /// </summary>
    /// <param name="token">The JWT security token.</param>
    /// <returns>True if the token is expired or expiration cannot be determined, false otherwise.</returns>
    public static bool IsTokenExpired(JwtSecurityToken? token)
    {
        DateTimeOffset? expiration = GetTokenExpiration(token);
        if (!expiration.HasValue)
        {
            return true; // Consider expired if we can't determine expiration
        }

        return expiration.Value < DateTimeOffset.Now;
    }

    /// <summary>
    /// Extracts the Bearer token from an Authorization header value.
    /// </summary>
    /// <param name="authorizationHeader">The Authorization header value (e.g., "Bearer {token}").</param>
    /// <returns>The token string without the "Bearer " prefix, or null if not found.</returns>
    public static string? ExtractBearerToken(string? authorizationHeader)
    {
        if (string.IsNullOrWhiteSpace(authorizationHeader))
        {
            return null;
        }

        const string BearerPrefix = "Bearer ";
        if (!authorizationHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string token = authorizationHeader.Substring(BearerPrefix.Length).Trim();
        return string.IsNullOrWhiteSpace(token) ? null : token;
    }

    /// <summary>
    /// Generates a cache key from the token by creating a SHA256 hash.
    /// This ensures we don't store the full token in cache keys for security.
    /// </summary>
    /// <param name="token">The JWT token string.</param>
    /// <returns>A cache key based on the token hash with the format "jwt_token:{hash}".</returns>
    public static string GenerateCacheKey(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            throw new ArgumentException("Token cannot be null or empty.", nameof(token));
        }

        byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
        byte[] hashBytes = SHA256.HashData(tokenBytes);
        string hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return $"{JwtTokenCacheKeyPrefix}{hashString}";
    }
}

