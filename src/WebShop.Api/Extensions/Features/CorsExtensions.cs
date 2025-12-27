using WebShop.Util.Models;

namespace WebShop.Api.Extensions.Features;

/// <summary>
/// Extension methods for configuring CORS policies.
/// </summary>
public static class CorsExtensions
{
    private const string CorsPolicyDevelopment = "AllowAll";
    private const string CorsPolicyRestricted = "Restricted";

    /// <summary>
    /// Configures CORS policies for both development and production environments.
    /// </summary>
    public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure development policy (AllowAll)
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyDevelopment, policy => policy
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());

            // Configure Restricted policy for dev/qa/uat/production environments 
            ConfigureRestrictedCorsPolicy(options, configuration);
        });
    }

    /// <summary>
    /// Configures the CORS policy with restricted settings from configuration.
    /// </summary>
    private static void ConfigureRestrictedCorsPolicy(Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions options, IConfiguration configuration)
    {
        CorsSettings corsSettings = new();
        configuration.GetSection("CorsOptions").Bind(corsSettings);

        options.AddPolicy(CorsPolicyRestricted, policy =>
        {
            // Configure allowed origins
            if (corsSettings.AllowedOrigins.Length > 0)
            {
                policy.WithOrigins(corsSettings.AllowedOrigins);
            }
            else
            {
                // Default: no origins allowed (most restrictive)
                policy.WithOrigins([]);
            }

            // Configure allowed methods
            if (corsSettings.AllowedMethods.Length > 0)
            {
                policy.WithMethods(corsSettings.AllowedMethods);
            }
            else
            {
                // Default: common HTTP methods
                policy.WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS");
            }

            // Configure allowed headers
            if (corsSettings.AllowedHeaders.Length > 0)
            {
                policy.WithHeaders(corsSettings.AllowedHeaders);
            }
            else
            {
                // Allow all headers when no specific headers are configured
                policy.AllowAnyHeader();
            }

            // Configure credentials
            if (corsSettings.AllowCredentials)
            {
                policy.AllowCredentials();
            }

            // Configure preflight cache
            if (corsSettings.MaxAgeSeconds.HasValue)
            {
                policy.SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.MaxAgeSeconds.Value));
            }
        });
    }

    /// <summary>
    /// Gets the appropriate CORS policy name based on the environment.
    /// </summary>
    public static string GetCorsPolicyName(this WebApplication app)
    {
        return app.Environment.IsDevelopment()
            ? CorsPolicyDevelopment
            : CorsPolicyRestricted;
    }
}
