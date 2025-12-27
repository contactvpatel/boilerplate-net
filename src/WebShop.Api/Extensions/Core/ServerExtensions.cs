using WebShop.Util.Models;

namespace WebShop.Api.Extensions.Core;

/// <summary>
/// Extension methods for configuring server-level settings (Kestrel, request limits, etc.).
/// </summary>
public static class ServerExtensions
{
    /// <summary>
    /// Configures Kestrel request size limits using HttpResilienceOptions.
    /// This prevents large request bodies from consuming excessive memory and protects against DoS attacks.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    public static void ConfigureKestrelRequestLimits(this WebApplicationBuilder builder)
    {
        // Bind HttpResilienceOptions to get configuration values
        HttpResilienceOptions options = new();
        builder.Configuration.GetSection("HttpResilienceOptions").Bind(options);

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            // Request body size limit (prevents DoS attacks via large payloads)
            serverOptions.Limits.MaxRequestBodySize = options.MaxRequestSizeBytes;

            // Request header limits (prevents header-based DoS attacks)
            serverOptions.Limits.MaxRequestHeadersTotalSize = options.MaxRequestHeadersTotalSize;
            serverOptions.Limits.MaxRequestHeaderCount = options.MaxRequestHeaderCount;
        });
    }
}
