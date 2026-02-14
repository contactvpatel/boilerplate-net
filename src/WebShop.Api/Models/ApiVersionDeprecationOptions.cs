namespace WebShop.Api.Models;

/// <summary>
/// Configuration options for API version deprecation headers.
/// </summary>
public class ApiVersionDeprecationOptions
{
    /// <summary>
    /// Gets the list of deprecated API versions with their deprecation information.
    /// </summary>
    public IReadOnlyList<DeprecatedVersion> DeprecatedVersions { get; set; } = [];
}

