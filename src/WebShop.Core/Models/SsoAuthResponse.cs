namespace WebShop.Core.Models;

/// <summary>
/// Response model containing SSO authentication information after token renewal.
/// </summary>
public class SsoAuthResponse
{
    /// <summary>
    /// New access token.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// New refresh token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration time in seconds.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// Token type (typically "Bearer").
    /// </summary>
    public string TokenType { get; set; } = "Bearer";
}

