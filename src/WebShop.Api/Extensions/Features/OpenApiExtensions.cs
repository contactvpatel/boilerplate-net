using Scalar.AspNetCore;
using WebShop.Api.Helpers;

namespace WebShop.Api.Extensions.Features;

/// <summary>
/// Extension methods for configuring OpenAPI and Scalar UI.
/// Follows official Scalar ASP.NET Core integration guidelines:
/// https://guides.scalar.com/scalar/scalar-api-references/integrations/net-aspnet-core/integration
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Configures OpenAPI services.
    /// </summary>
    public static void ConfigureOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi();
    }

    /// <summary>
    /// Configures OpenAPI endpoints and Scalar UI.
    /// Following the basic setup from Scalar documentation for Microsoft.AspNetCore.OpenApi.
    /// </summary>
    public static void ConfigureOpenApiEndpoints(this WebApplication app)
    {
        string? environment = app.Configuration.GetValue<string>("AppSettings:Environment");

        // Show Scalar UI for all environments except Production
        if (!string.Equals(environment, "Production", StringComparison.OrdinalIgnoreCase))
        {
            // Basic setup as per Scalar documentation
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        // Register transformation middleware AFTER endpoints are mapped
        // This ensures it can intercept OpenAPI responses
        app.UseOpenApiTransformationMiddleware();
    }

    /// <summary>
    /// Adds middleware to transform OpenAPI document JSON response.
    /// Replaces version placeholders (v{version} and {version}) with actual version (v1 and 1).
    /// </summary>
    private static void UseOpenApiTransformationMiddleware(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            // Only intercept OpenAPI document requests
            if (!context.Request.Path.StartsWithSegments("/openapi"))
            {
                await next();
                return;
            }

            Stream originalBodyStream = context.Response.Body;
            using MemoryStream responseBody = new();
            context.Response.Body = responseBody;

            await next();

            // Transform OpenAPI JSON responses to replace version placeholders
            if (ShouldTransformOpenApiResponse(context))
            {
                responseBody.Seek(0, SeekOrigin.Begin);
                string originalJson = await new StreamReader(responseBody).ReadToEndAsync();
                string transformedJson = OpenApiTransformer.Transform(originalJson);

                // Update Content-Length header
                context.Response.ContentLength = System.Text.Encoding.UTF8.GetByteCount(transformedJson);

                // Write transformed content
                responseBody.SetLength(0);
                await responseBody.WriteAsync(System.Text.Encoding.UTF8.GetBytes(transformedJson));
            }

            responseBody.Seek(0, SeekOrigin.Begin);
            await responseBody.CopyToAsync(originalBodyStream);
            context.Response.Body = originalBodyStream;
        });
    }

    /// <summary>
    /// Determines if the OpenAPI response should be transformed.
    /// </summary>
    private static bool ShouldTransformOpenApiResponse(HttpContext context)
    {
        // Only transform successful OpenAPI document responses
        return context.Request.Path.StartsWithSegments("/openapi") &&
               context.Response.StatusCode == 200 &&
               (context.Response.ContentType?.Contains("application/json") == true ||
                context.Response.ContentType?.Contains("application/openapi+json") == true);
    }
}
