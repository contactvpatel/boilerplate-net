namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for response compression.
/// </summary>
public class ResponseCompressionSettings
{
    /// <summary>
    /// Gets or sets whether response compression is enabled.
    /// Default: true (enabled for production performance).
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets whether compression is enabled for HTTPS connections.
    /// Note: This application blocks all HTTP requests (returns 400), so all requests are HTTPS.
    /// Default: true (always enabled since all requests are HTTPS).
    /// This setting is kept for consistency but is always true in this application.
    /// </summary>
    public bool EnableForHttps { get; set; } = true;

    /// <summary>
    /// Gets or sets the minimum response size in bytes before compression is applied.
    /// Responses smaller than this will not be compressed (saves CPU for small responses).
    /// Default: 1024 bytes (1 KB).
    /// Microsoft recommendation: 860-1024 bytes.
    /// </summary>
    public int MinimumResponseSizeBytes { get; set; } = 1024;

    /// <summary>
    /// Gets or sets the Brotli compression level (0-11).
    /// Higher values = better compression but more CPU usage.
    /// Default: 4 (balanced - Microsoft recommendation for optimal performance).
    /// Range: 0 (fastest) to 11 (best compression).
    /// </summary>
    public int BrotliCompressionLevel { get; set; } = 4;

    /// <summary>
    /// Gets or sets the Gzip compression level (0-9).
    /// Higher values = better compression but more CPU usage.
    /// Default: 4 (balanced - Microsoft recommendation for optimal performance).
    /// Range: 0 (fastest) to 9 (best compression).
    /// </summary>
    public int GzipCompressionLevel { get; set; } = 4;

    /// <summary>
    /// Gets or sets whether to use Brotli compression (preferred, better compression ratio).
    /// Default: true.
    /// </summary>
    public bool UseBrotli { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to use Gzip compression (fallback for older clients).
    /// Default: true.
    /// </summary>
    public bool UseGzip { get; set; } = true;

    /// <summary>
    /// Gets or sets additional MIME types to compress beyond the defaults.
    /// Default MIME types are already included: text/*, application/json, application/xml, etc.
    /// </summary>
    public List<string> AdditionalMimeTypes { get; set; } = new();
}

