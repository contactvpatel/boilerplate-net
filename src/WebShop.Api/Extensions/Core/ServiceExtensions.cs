using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using WebShop.Api.Extensions.Features;
using WebShop.Api.Extensions.Utilities;
using WebShop.Api.Models;
using WebShop.Business;
using WebShop.Infrastructure;
using WebShop.Util.Models;

namespace WebShop.Api.Extensions.Core;

/// <summary>
/// Extension methods for configuring services in the API layer.
/// This is the main orchestrator that delegates to focused extension classes.
/// </summary>
public static class ServiceExtensions
{
    /// <summary>
    /// Configures all API services. This is the main entry point that orchestrates service configuration.
    /// </summary>
    public static void ConfigureApiServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
    {
        services.ConfigureApplicationSettings(configuration);
        services.ConfigureApiLayerServices(configuration);
        services.ConfigureCors(configuration);
        services.ConfigureInfrastructureServices(configuration, environment);
        services.ConfigureBusinessServices();
    }

    /// <summary>
    /// Configures API layer specific services (versioning, controllers, OpenAPI, health checks).
    /// </summary>
    private static void ConfigureApiLayerServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Rate limiting must be configured early (before other middleware)
        services.ConfigureRateLimiting(configuration);

        services.ConfigureApiVersioning();
        services.ConfigureApiVersionDeprecation(); // Configure API version deprecation options
        services.ConfigureResponseCompression(configuration); // Configure response compression
        services.ConfigureResponseCaching(); // Configure response caching
        services.ConfigureControllers();
        services.ConfigureOpenApi();
        services.AddHttpContextAccessor();
        services.ConfigureHealthChecks();
        services.ConfigureRequestSizeLimits();
        services.ConfigureSecurityHeaders(configuration);
        services.ConfigureFilterServices(); // Configure filter-related services
    }

    /// <summary>
    /// Configures security headers options from configuration.
    /// </summary>
    private static void ConfigureSecurityHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SecurityHeadersSettings>()
            .BindConfiguration("SecurityHeaders")
            .ValidateOnStart();
    }

    /// <summary>
    /// Configures API version deprecation options from configuration.
    /// </summary>
    private static void ConfigureApiVersionDeprecation(this IServiceCollection services)
    {
        services.AddOptions<ApiVersionDeprecationOptions>()
            .BindConfiguration("ApiVersionDeprecationOptions")
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    /// Configures infrastructure services (database, repositories, migrations).
    /// </summary>
    private static void ConfigureInfrastructureServices(this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment)
    {
        // Register startup filters (execute in reverse order of registration)
        // Register migrations first, then validation, so validation executes first
        services.AddTransient<IStartupFilter, Filters.DatabaseMigrationInitFilter>();
        services.AddTransient<IStartupFilter, Filters.DatabaseConnectionValidationFilter>();

        services.AddInfrastructure(configuration, environment);
    }

    /// <summary>
    /// Configures business services (application layer services, DTOs, mappings).
    /// </summary>
    private static void ConfigureBusinessServices(this IServiceCollection services)
    {
        services.AddBusinessServices();
    }

    /// <summary>
    /// Configures request size limits for form data and multipart requests.
    /// Uses HttpResilienceOptions for consistent configuration across all layers.
    /// </summary>
    private static void ConfigureRequestSizeLimits(this IServiceCollection services)
    {
        services.AddOptions<FormOptions>()
            .Configure<IOptions<HttpResilienceOptions>>((formOptions, resilienceOptions) =>
            {
                // Configure form options using HttpResilienceOptions for consistency
                formOptions.MultipartBodyLengthLimit = resilienceOptions.Value.MaxRequestSizeBytes;
                formOptions.ValueLengthLimit = resilienceOptions.Value.MaxRequestSizeBytes;
                formOptions.ValueCountLimit = resilienceOptions.Value.MaxFormValueCount;
            });
    }
}
