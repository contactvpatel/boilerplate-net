using System.ComponentModel.DataAnnotations;

namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for MIS service.
/// </summary>
public class MisServiceOptions
{
    /// <summary>
    /// MIS service base URL.
    /// </summary>
    [Required]
    [Url(ErrorMessage = "MisService Url must be a valid URL.")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds.
    /// Default: 30 seconds.
    /// </summary>
    [Range(1, 30, ErrorMessage = "TimeoutSeconds must be between 1 and 30 seconds.")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// MIS service headers for authentication.
    /// </summary>
    [Required]
    public MisServiceHeaders Headers { get; set; } = new();

    /// <summary>
    /// MIS service endpoint paths.
    /// </summary>
    [Required]
    public MisServiceEndpoints Endpoint { get; set; } = new();
}

