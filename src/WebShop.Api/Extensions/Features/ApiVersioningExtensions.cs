using Asp.Versioning;

namespace WebShop.Api.Extensions.Features;

/// <summary>
/// Extension methods for configuring API versioning.
/// </summary>
public static class ApiVersioningExtensions
{
    /// <summary>
    /// Configures API versioning (major version only, e.g., 1, 2, 3).
    /// Supports versioning via URL segment (primary) and HTTP header (api-version).
    /// </summary>
    public static void ConfigureApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("api-version")
            );
        });
    }
}
