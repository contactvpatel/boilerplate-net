namespace WebShop.Api.Models;

/// <summary>
/// Configuration for a deprecated API version.
/// </summary>
public class DeprecatedVersion
{
    /// <summary>
    /// Gets or sets the major version number (e.g., 1, 2, 3).
    /// </summary>
    public int MajorVersion { get; set; }

    /// <summary>
    /// Gets or sets whether this version is deprecated.
    /// </summary>
    public bool IsDeprecated { get; set; }

    /// <summary>
    /// Gets or sets the sunset date when this version will be removed (RFC 7231 format: "Mon, 01 Jan 2025 00:00:00 GMT").
    /// Optional - if not set, no Sunset header will be added.
    /// </summary>
    public string? SunsetDate { get; set; }

    /// <summary>
    /// Gets or sets the URL of the successor version (e.g., "/api/v2").
    /// Optional - if not set, no Link header will be added.
    /// </summary>
    public string? SuccessorVersionUrl { get; set; }

    /// <summary>
    /// Gets or sets an optional deprecation message to include in the Deprecation header.
    /// If not set, "true" will be used as the default value.
    /// </summary>
    public string? DeprecationMessage { get; set; }
}
