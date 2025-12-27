namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for ASM service.
/// </summary>
public class AsmServiceOptions
{
    /// <summary>
    /// ASM service base URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds.
    /// Default: 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// ASM service headers for authentication.
    /// </summary>
    public AsmServiceHeaders Headers { get; set; } = new();

    /// <summary>
    /// ASM service endpoint paths.
    /// </summary>
    public AsmServiceEndpoints Endpoint { get; set; } = new();
}

