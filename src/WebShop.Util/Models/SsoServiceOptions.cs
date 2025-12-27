namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for SSO service.
/// </summary>
public class SsoServiceOptions
{
    /// <summary>
    /// SSO service base URL.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout in seconds.
    /// Default: 30 seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// SSO service endpoint paths.
    /// </summary>
    public SsoServiceEndpoints Endpoint { get; set; } = new();
}

