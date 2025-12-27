namespace WebShop.Core.Models;

/// <summary>
/// Request model for validating a token with the SSO service.
/// </summary>
public class ValidateTokenRequest
{
    /// <summary>
    /// The token to validate.
    /// </summary>
    public string Token { get; set; } = string.Empty;
}
