using Asp.Versioning;
using Microsoft.Extensions.Options;
using WebShop.Api.Models;

namespace WebShop.Api.Middleware;

/// <summary>
/// Middleware that adds deprecation headers to responses for deprecated API versions.
/// Implements RFC 8594 (Deprecation HTTP Header) and RFC 8595 (Sunset HTTP Header).
/// </summary>
/// <param name="next">The next middleware in the pipeline.</param>
/// <param name="options">The API version deprecation options.</param>
public class ApiVersionDeprecationMiddleware(RequestDelegate next, IOptions<ApiVersionDeprecationOptions> options)
{
    private readonly RequestDelegate _next = next ?? throw new ArgumentNullException(nameof(next));
    private readonly ApiVersionDeprecationOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Invokes the middleware to add deprecation headers if the requested API version is deprecated.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        // Only add headers for API endpoints (not health checks, OpenAPI, etc.)
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        // Get the requested API version
        ApiVersion? requestedVersion = context.GetRequestedApiVersion();

        if (requestedVersion == null)
        {
            return;
        }

        // Find if this version is deprecated
        DeprecatedVersion? deprecatedVersion = _options.DeprecatedVersions
            .FirstOrDefault(v => v.MajorVersion == requestedVersion.MajorVersion && v.IsDeprecated);

        if (deprecatedVersion == null)
        {
            return;
        }

        // Add Deprecation header (RFC 8594)
        // Format: "Deprecation: true" or "Deprecation: <date-time>"
        string deprecationValue = string.IsNullOrWhiteSpace(deprecatedVersion.DeprecationMessage)
            ? "true"
            : deprecatedVersion.DeprecationMessage;

        if (!context.Response.Headers.ContainsKey("Deprecation"))
        {
            context.Response.Headers.Append("Deprecation", deprecationValue);
        }

        // Add Sunset header (RFC 8595) if sunset date is specified
        if (!string.IsNullOrWhiteSpace(deprecatedVersion.SunsetDate))
        {
            if (!context.Response.Headers.ContainsKey("Sunset"))
            {
                context.Response.Headers.Append("Sunset", deprecatedVersion.SunsetDate);
            }
        }

        // Add Link header with successor version (RFC 8288) if specified
        if (!string.IsNullOrWhiteSpace(deprecatedVersion.SuccessorVersionUrl))
        {
            string linkValue = $"<{deprecatedVersion.SuccessorVersionUrl}>; rel=\"successor-version\"";

            if (context.Response.Headers.ContainsKey("Link"))
            {
                // Append to existing Link header
                string existingLink = context.Response.Headers["Link"].ToString();
                context.Response.Headers["Link"] = $"{existingLink}, {linkValue}";
            }
            else
            {
                context.Response.Headers.Append("Link", linkValue);
            }
        }
    }
}

