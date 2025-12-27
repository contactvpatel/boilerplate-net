namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for MIS service.
/// </summary>
public class MisServiceOptions
{
    /// <summary>
    /// MIS service base URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds.
    /// Default: 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// MIS service headers for authentication.
    /// </summary>
    public MisServiceHeaders Headers { get; set; } = new();

    /// <summary>
    /// MIS service endpoint paths.
    /// </summary>
    public MisServiceEndpoints Endpoint { get; set; } = new();
}

