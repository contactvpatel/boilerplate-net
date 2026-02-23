using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using WebShop.Util;

namespace WebShop.Api.Helpers;

/// <summary>
/// Helper class for transforming OpenAPI document JSON responses.
/// </summary>
public static class OpenApiTransformer
{
    /// <summary>
    /// Builds the Scalar UI title from configuration: "App | Version | Build #".
    /// </summary>
    public static string GetScalarTitle(IConfiguration configuration)
    {
        string? appName = configuration.GetValue<string>(ConfigurationKeys.AppSettingsApplicationName)
            ?? configuration.GetValue<string>(ConfigurationKeys.AppSettingsApplicationName);
        string? version = configuration.GetValue<string>(ConfigurationKeys.AppSettingsApplicationVersion);
        string? buildNumber = configuration.GetValue<string>(ConfigurationKeys.AppBuildNumber);
        return BuildScalarTitle(appName, version, buildNumber);
    }
    /// <summary>
    /// Transforms the OpenAPI JSON by replacing version placeholders, setting default values for path parameters,
    /// and setting the info title for Scalar UI in format "App | Version | Build #".
    /// </summary>
    /// <param name="json">The original OpenAPI JSON.</param>
    /// <param name="appName">Application name (e.g. WebShop.Api).</param>
    /// <param name="version">Application version (e.g. 1.0.0).</param>
    /// <param name="buildNumber">Optional build number from APP_BUILD_NUMBER.</param>
    /// <returns>The transformed OpenAPI JSON.</returns>
    public static string Transform(string json, string? appName = null, string? version = null, string? buildNumber = null)
    {
        // Set info.title for Scalar UI: "App | Version | Build # 123"
        string title = BuildScalarTitle(appName, version, buildNumber);
        if (!string.IsNullOrEmpty(title))
        {
            string escapedTitle = title.Replace("\\", "\\\\").Replace("\"", "\\\"");
            json = Regex.Replace(
                json,
                @"(""info""\s*:\s*\{[^}]*?""title""\s*:\s*)""[^""]*""",
                m => $"{m.Groups[1].Value}\"{escapedTitle}\"",
                RegexOptions.Singleline);
        }

        // Replace version placeholders with default version (1) in paths
        json = json.Replace("v{version}", "v1").Replace("{version}", "1");

        // Add default value for version path parameter in the OpenAPI spec
        // Scalar will use this default value to auto-fill the version parameter in the UI
        // Pattern 1: Matches version path parameter and adds default value
        // Uses non-greedy match ([^}]*?) to handle any property order in the JSON object
        json = Regex.Replace(
            json,
            @"""name""\s*:\s*""version""([^}]*?)""in""\s*:\s*""path""([^}]*?)(})",
            @"""name"":""version""$1""in"":""path""$2,""default"":""1""$3",
            RegexOptions.IgnoreCase);

        // Pattern 2: Handle edge case where default might already exist but is empty/null
        // Replace empty or null default values with "1"
        json = Regex.Replace(
            json,
            @"""name""\s*:\s*""version""([^}]*?)""default""\s*:\s*(?:""""|null|""[^""]*"")",
            @"""name"":""version""$1""default"":""1""",
            RegexOptions.IgnoreCase);

        return json;
    }

    private static string BuildScalarTitle(string? appName, string? version, string? buildNumber)
    {
        List<string> parts = new List<string>();
        if (!string.IsNullOrEmpty(appName))
        {
            parts.Add(appName);
        }

        if (!string.IsNullOrEmpty(version))
        {
            string versionDisplay = version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}";
            parts.Add(versionDisplay);
        }
        if (!string.IsNullOrEmpty(buildNumber))
        {
            parts.Add($"Build # {buildNumber}");
        }

        return parts.Count > 0 ? string.Join(" | ", parts) : string.Empty;
    }
}

