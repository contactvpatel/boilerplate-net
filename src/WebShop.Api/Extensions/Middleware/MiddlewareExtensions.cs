using WebShop.Api.Extensions.Features;

namespace WebShop.Api.Extensions.Middleware;

/// <summary>
/// Extension methods for configuring middleware pipeline.
/// </summary>
public static class MiddlewareExtensions
{
    /// <summary>
    /// Configures the middleware pipeline for the application.
    /// </summary>
    public static void ConfigureMiddleware(this WebApplication app)
    {
        app.EnforceHttps();

        // Rate limiting should be early in pipeline (after HTTPS, before other middleware)
        // This prevents resource consumption from rate-limited requests
        app.UseRateLimiter();

        // Response compression must be early in pipeline (before UseRouting)
        // This ensures responses are compressed before being sent to client
        app.UseResponseCompressionIfEnabled();

        // Response caching must be after compression but before authorization
        // This ensures cached responses are compressed and served without hitting authorization
        app.UseResponseCachingMiddleware();

        app.UseExceptionHandling(options =>
        {
            options.AddResponseDetails = UpdateApiErrorResponse;
            options.DetermineLogLevel = DetermineLogLevel;
        });
        app.UseApiVersionDeprecation(); // Add deprecation headers for deprecated API versions
        app.UseCors(app.GetCorsPolicyName());
        app.UseAuthorization();
        app.MapControllers();
    }

    /// <summary>
    /// Adds API version deprecation middleware to the pipeline.
    /// </summary>
    private static void UseApiVersionDeprecation(this WebApplication app)
    {
        app.UseMiddleware<Api.Middleware.ApiVersionDeprecationMiddleware>();
    }

    /// <summary>
    /// Determines the log level based on exception type and message.
    /// </summary>
    private static LogLevel DetermineLogLevel(Exception ex)
    {
        // Database connection errors should be logged as Critical
        if (ex.Message.Contains("error occurred using the connection to database", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("a network-related", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("connection to the database", StringComparison.OrdinalIgnoreCase))
        {
            return LogLevel.Critical;
        }

        // Operation canceled exceptions are informational (expected behavior)
        if (ex is OperationCanceledException)
        {
            return LogLevel.Information;
        }

        // Argument exceptions are warnings (client errors)
        if (ex is ArgumentException)
        {
            return LogLevel.Warning;
        }

        // Not found exceptions are informational (expected in some cases)
        if (ex is KeyNotFoundException ||
            ex is InvalidOperationException && ex.Message.Contains("not found", StringComparison.OrdinalIgnoreCase))
        {
            return LogLevel.Information;
        }

        // Default to Error for unexpected exceptions
        return LogLevel.Error;
    }

    /// <summary>
    /// Updates the API error response with additional details based on exception type.
    /// </summary>
    private static void UpdateApiErrorResponse(HttpContext context, Exception ex, Models.Response<Models.ApiError> apiError)
    {
        // Add database-specific error handling if needed
        if (ex.GetType().Name.Contains("PostgresException", StringComparison.OrdinalIgnoreCase) ||
            ex.GetType().Name.Contains("SqlException", StringComparison.OrdinalIgnoreCase))
        {
            apiError.Message = "A database error occurred. Please contact support if the problem persists.";
        }
    }

    /// <summary>
    /// Enforces HTTPS-only access. Blocks all HTTP requests and adds HSTS headers.
    /// </summary>
    private static void EnforceHttps(this WebApplication app)
    {
        // Block HTTP requests - return 400 Bad Request
        app.Use(async (context, next) =>
        {
            if (!context.Request.IsHttps)
            {
                context.Response.StatusCode = 400;
                await context.Response.WriteAsync("HTTPS is required. HTTP requests are not allowed.");
                return;
            }
            await next();
        });

        // Add HSTS (HTTP Strict Transport Security) headers in production
        if (app.Environment.IsProduction())
        {
            app.UseHsts();
        }
    }
}
