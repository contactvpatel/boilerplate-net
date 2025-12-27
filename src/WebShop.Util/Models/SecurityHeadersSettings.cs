namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for security headers including Content-Security-Policy.
/// </summary>
public class SecurityHeadersSettings
{
    /// <summary>
    /// Whether security headers are enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Content-Security-Policy header value.
    /// Includes frame-ancestors directive for clickjacking protection.
    /// Default: "default-src 'self'; frame-ancestors 'none'"
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'; frame-ancestors 'none'";

    /// <summary>
    /// X-Content-Type-Options header value.
    /// Default: "nosniff"
    /// </summary>
    public string XContentTypeOptions { get; set; } = "nosniff";

    /// <summary>
    /// Referrer-Policy header value.
    /// Default: "strict-origin-when-cross-origin"
    /// </summary>
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
}

