using WebShop.Util.Models;

namespace WebShop.Api.Extensions.Features;

/// <summary>
/// Extension methods for configuring security headers including Content-Security-Policy.
/// </summary>
public static class SecurityHeadersExtensions
{
    private const string DefaultCspPolicy = "default-src 'self'; frame-ancestors 'none'";

    /// <summary>
    /// Adds security headers middleware to the pipeline.
    /// Should be called early in the middleware pipeline, after HTTPS enforcement.
    /// </summary>
    public static void UseSecurityHeaders(this WebApplication app)
    {
        SecurityHeadersSettings settings = new();
        app.Configuration.GetSection("SecurityHeaders").Bind(settings);

        // Use defaults if configuration is missing
        if (string.IsNullOrWhiteSpace(settings.ContentSecurityPolicy))
        {
            settings.ContentSecurityPolicy = DefaultCspPolicy;
        }

        if (string.IsNullOrWhiteSpace(settings.XContentTypeOptions))
        {
            settings.XContentTypeOptions = "nosniff";
        }

        if (string.IsNullOrWhiteSpace(settings.ReferrerPolicy))
        {
            settings.ReferrerPolicy = "strict-origin-when-cross-origin";
        }

        // Skip adding headers if disabled
        if (!settings.Enabled)
        {
            return;
        }

        app.Use(async (context, next) =>
        {
            // CSP: Environment-specific policy
            string cspPolicy = GetCspPolicy(app.Environment, settings.ContentSecurityPolicy);

            context.Response.Headers.Append("Content-Security-Policy", cspPolicy);
            context.Response.Headers.Append("X-Content-Type-Options", settings.XContentTypeOptions);
            context.Response.Headers.Append("Referrer-Policy", settings.ReferrerPolicy);

            await next();
        });
    }

    /// <summary>
    /// Gets the appropriate CSP policy based on environment.
    /// </summary>
    private static string GetCspPolicy(IWebHostEnvironment environment, string configuredPolicy)
    {
        string policy = configuredPolicy;

        // Non-Production: Use permissive policy for Scalar UI compatibility
        if (!environment.IsProduction())
        {
            // Scalar UI requires inline styles and scripts
            // If policy doesn't explicitly set script-src or style-src, use Scalar-compatible policy
            if (!policy.Contains("script-src", StringComparison.OrdinalIgnoreCase) ||
                !policy.Contains("style-src", StringComparison.OrdinalIgnoreCase))
            {
                // Build permissive policy for Scalar UI
                policy = "default-src 'self'; " +
                        "script-src 'self' 'unsafe-inline' 'unsafe-eval'; " +
                        "style-src 'self' 'unsafe-inline'; " +
                        "img-src 'self' data: https:; " +
                        "font-src 'self' data:; " +
                        "frame-ancestors 'none'";
            }

            // Add report-uri for monitoring violations (if not already present)
            if (!policy.Contains("report-uri", StringComparison.OrdinalIgnoreCase))
            {
                policy = $"{policy}; report-uri /api/csp-report";
            }
        }

        return policy;
    }
}

