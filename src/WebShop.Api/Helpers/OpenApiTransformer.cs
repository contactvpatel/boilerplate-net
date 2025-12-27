using System.Text.RegularExpressions;

namespace WebShop.Api.Helpers;

/// <summary>
/// Helper class for transforming OpenAPI document JSON responses.
/// </summary>
public static class OpenApiTransformer
{
    /// <summary>
    /// Transforms the OpenAPI JSON by replacing version placeholders and setting default values for path parameters.
    /// This ensures Scalar can auto-fill the version path parameter.
    /// </summary>
    /// <param name="json">The original OpenAPI JSON.</param>
    /// <returns>The transformed OpenAPI JSON.</returns>
    public static string Transform(string json)
    {
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
}

