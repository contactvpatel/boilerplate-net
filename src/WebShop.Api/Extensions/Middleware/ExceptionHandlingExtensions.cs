using WebShop.Api.Middleware;
using WebShop.Api.Models;

namespace WebShop.Api.Extensions.Middleware;

/// <summary>
/// Extension methods for configuring exception handling middleware.
/// </summary>
public static class ExceptionHandlingExtensions
{
    /// <summary>
    /// Adds exception handling middleware to the pipeline with custom options.
    /// </summary>
    /// <param name="app">The application builder.</param>
    /// <param name="configureOptions">Action to configure exception handling options.</param>
    /// <returns>The application builder for chaining.</returns>
    public static IApplicationBuilder UseExceptionHandling(
        this IApplicationBuilder app,
        Action<ExceptionHandlingOptions> configureOptions)
    {
        ExceptionHandlingOptions options = new();
        configureOptions(options);
        return app.UseMiddleware<ExceptionHandlingMiddleware>(options);
    }
}
