namespace WebShop.Api.Models;

/// <summary>
/// Configuration options for API version deprecation headers.
/// </summary>
public class ApiVersionDeprecationOptions
{
    /// <summary>
    /// Gets or sets the list of deprecated API versions with their deprecation information.
    /// </summary>
    public List<DeprecatedVersion> DeprecatedVersions { get; set; } = new();
}

