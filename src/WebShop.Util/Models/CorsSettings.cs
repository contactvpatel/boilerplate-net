namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for CORS (Cross-Origin Resource Sharing).
/// </summary>
public class CorsSettings
{
    /// <summary>
    /// List of allowed origins for production environment.
    /// </summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// List of allowed HTTP methods for production environment.
    /// If empty, defaults to common methods (GET, POST, PUT, DELETE, PATCH, OPTIONS).
    /// </summary>
    public string[] AllowedMethods { get; set; } = Array.Empty<string>();

    /// <summary>
    /// List of allowed headers for production environment.
    /// If empty, defaults to common headers (Content-Type, Authorization, X-Requested-With).
    /// </summary>
    public string[] AllowedHeaders { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Whether to allow credentials (cookies, authorization headers) in production.
    /// </summary>
    public bool AllowCredentials { get; set; } = false;

    /// <summary>
    /// Maximum age (in seconds) for preflight requests cache.
    /// </summary>
    public int? MaxAgeSeconds { get; set; }
}

