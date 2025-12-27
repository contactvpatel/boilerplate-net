namespace WebShop.Util.Models;

/// <summary>
/// SSO service endpoint paths.
/// </summary>
public class SsoServiceEndpoints
{
    /// <summary>
    /// Endpoint for token validation.
    /// </summary>
    public string ValidateToken { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for token renewal.
    /// </summary>
    public string RenewToken { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for user logout.
    /// </summary>
    public string Logout { get; set; } = string.Empty;
}
