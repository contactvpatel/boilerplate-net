namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Centralized HTTP header names for external service authentication.
/// </summary>
public static class HttpHeaderNames
{
    /// <summary>
    /// ASM service authentication app ID header.
    /// </summary>
    public const string AsmAuthAppId = "x-app-auth-id";

    /// <summary>
    /// ASM service authentication app secret header.
    /// </summary>
    public const string AsmAuthAppSecret = "x-app-auth-secret";

    /// <summary>
    /// MIS service authentication app ID header.
    /// </summary>
    public const string MisAuthAppId = "x-baps-auth-app-id";

    /// <summary>
    /// MIS service authentication app secret header.
    /// </summary>
    public const string MisAuthAppSecret = "x-baps-auth-app-secret";
}
