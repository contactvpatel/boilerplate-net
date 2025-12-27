using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Request DTO for renewing an SSO authentication token.
/// </summary>
public class SsoRenewTokenRequest
{
    /// <summary>
    /// Refresh token used to obtain a new access token.
    /// </summary>
    [Required]
    [StringLength(500, MinimumLength = 1, ErrorMessage = "Refresh token must be between 1 and 500 characters.")]
    public string RefreshToken { get; set; } = string.Empty;
}

