using System.ComponentModel.DataAnnotations;

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
    [MinLength(1, ErrorMessage = "ContentSecurityPolicy cannot be empty when security headers are used.")]
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'; frame-ancestors 'none'";

    /// <summary>
    /// X-Content-Type-Options header value.
    /// Default: "nosniff"
    /// </summary>
    [MinLength(1, ErrorMessage = "XContentTypeOptions cannot be empty.")]
    public string XContentTypeOptions { get; set; } = "nosniff";

    /// <summary>
    /// Referrer-Policy header value.
    /// Default: "strict-origin-when-cross-origin"
    /// </summary>
    [MinLength(1, ErrorMessage = "ReferrerPolicy cannot be empty.")]
    public string ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";
}

