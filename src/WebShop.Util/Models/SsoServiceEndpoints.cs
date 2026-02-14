using System.ComponentModel.DataAnnotations;

namespace WebShop.Util.Models;

/// <summary>
/// SSO service endpoint paths.
/// </summary>
public class SsoServiceEndpoints
{
    /// <summary>
    /// Endpoint for token validation.
    /// </summary>
    [Required]
    public string ValidateToken { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for token renewal.
    /// </summary>
    [Required]
    public string RenewToken { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for user logout.
    /// </summary>
    [Required]
    public string Logout { get; set; } = string.Empty;
}
