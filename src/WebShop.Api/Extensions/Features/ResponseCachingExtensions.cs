using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace WebShop.Api.Extensions.Features;

/// <summary>
/// Extension methods for configuring response caching in the application.
/// Response caching reduces server load by caching HTTP responses at the client, proxy, or server level.
/// </summary>
public static class ResponseCachingExtensions
{
    /// <summary>
    /// Configures response caching services.
    /// Response caching stores HTTP responses for a period of time and serves them from cache for subsequent identical requests.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <remarks>
    /// Response caching is best suited for:
    /// - Read-only GET endpoints
    /// - Data that doesn't change frequently (reference data, public content)
    /// - Endpoints that are called repeatedly with the same parameters
    /// 
    /// Do NOT use response caching for:
    /// - POST, PUT, DELETE endpoints
    /// - Endpoints that return user-specific data
    /// - Endpoints that return frequently changing data
    /// - Authenticated endpoints (without careful consideration)
    /// </remarks>
    public static void ConfigureResponseCaching(this IServiceCollection services)
    {
        services.AddResponseCaching(options =>
        {
            // Maximum size of cacheable responses (1MB default)
            // Responses larger than this will not be cached
            options.MaximumBodySize = 1024 * 1024; // 1MB

            // Use case-sensitive paths for caching
            // /api/customers and /api/Customers are treated as different
            options.UseCaseSensitivePaths = true;

            // Size limit for response cache middleware (100MB)
            // This is the total memory allocated for the cache
            options.SizeLimit = 100 * 1024 * 1024; // 100MB
        });
    }

    /// <summary>
    /// Enables response caching middleware in the application pipeline.
    /// Must be placed early in the pipeline, before UseAuthorization() and endpoint routing.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <remarks>
    /// Middleware ordering is critical:
    /// 1. UseResponseCaching() should be placed AFTER compression but BEFORE authorization
    /// 2. This ensures cached responses are served without hitting authorization logic
    /// 3. Cache responses are stored after compression (smaller cache entries)
    /// 
    /// Important: Response caching respects Cache-Control headers.
    /// If an endpoint sets Cache-Control: no-cache or no-store, it won't be cached.
    /// </remarks>
    public static void UseResponseCachingMiddleware(this IApplicationBuilder app)
    {
        app.UseResponseCaching();
    }
}
