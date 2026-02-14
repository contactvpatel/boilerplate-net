using System.ComponentModel.DataAnnotations;

namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for ASM service.
/// </summary>
public class AsmServiceOptions
{
    /// <summary>
    /// ASM service base URL.
    /// </summary>
    [Required]
    [Url(ErrorMessage = "AsmService Url must be a valid URL.")]
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds.
    /// Default: 30 seconds.
    /// </summary>
    [Range(1, 30, ErrorMessage = "TimeoutSeconds must be between 1 and 30 seconds.")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// ASM service headers for authentication.
    /// </summary>
    [Required]
    public AsmServiceHeaders Headers { get; set; } = new();

    /// <summary>
    /// ASM service endpoint paths.
    /// </summary>
    [Required]
    public AsmServiceEndpoints Endpoint { get; set; } = new();
}

