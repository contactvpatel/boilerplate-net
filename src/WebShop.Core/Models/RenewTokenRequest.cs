namespace WebShop.Core.Models;

/// <summary>
/// Request model for renewing a token with the SSO service.
/// </summary>
public class RenewTokenRequest
{
    /// <summary>
    /// The refresh token used to obtain a new access token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}
