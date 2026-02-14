using System.ComponentModel.DataAnnotations;

namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for SSO service.
/// </summary>
public class SsoServiceOptions
{
    /// <summary>
    /// SSO service base URL.
    /// </summary>
    [Required]
    [Url(ErrorMessage = "SsoService Url must be a valid URL.")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds.
    /// Default: 30 seconds.
    /// </summary>
    [Range(1, 30, ErrorMessage = "TimeoutSeconds must be between 1 and 30 seconds.")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// SSO service endpoint paths.
    /// </summary>
    [Required]
    public SsoServiceEndpoints Endpoint { get; set; } = new();
}

