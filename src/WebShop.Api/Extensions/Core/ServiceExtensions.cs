using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using WebShop.Api.Extensions.Features;
using WebShop.Api.Extensions.Utilities;
using WebShop.Api.Middleware;
using WebShop.Api.Models;
using WebShop.Business;
using WebShop.Infrastructure;
using WebShop.Util;
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
    public static void ConfigureApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureApplicationSettings(configuration);
        services.ConfigureApiLayerServices(configuration);
        services.ConfigureCors(configuration);
        services.ConfigureInfrastructureServices(configuration);
        services.ConfigureBusinessServices();
    }

    /// <summary>
    /// Configures API layer specific services (versioning, controllers, OpenAPI, health checks).
    /// </summary>
    private static void ConfigureApiLayerServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureSecurityServices(configuration);

        services.ConfigureApiVersioning();
        services.ConfigureApiVersionDeprecation();
        services.ConfigureResponseCompression(configuration);
        services.ConfigureResponseCaching();
        services.ConfigureControllers();
        services.ConfigureOpenApi();
        services.AddHttpContextAccessor();
        services.ConfigureHealthChecks();
        services.ConfigureFilterServices();
        services.AddSingleton<ExceptionHandlingStrategy>();
    }

    /// <summary>
    /// Configures security-related services: rate limiting, security headers, and request size limits.
    /// All inbound security configurations in one place.
    /// </summary>
    private static void ConfigureSecurityServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureRateLimiting(configuration);
        services.ConfigureSecurityHeaders(configuration);
        services.ConfigureRequestSizeLimits();
    }

    /// <summary>
    /// Configures security headers options from configuration.
    /// </summary>
    private static void ConfigureSecurityHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SecurityHeadersSettings>()
            .BindConfiguration(ConfigurationKeys.SecurityHeaders)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    /// Configures API version deprecation options from configuration.
    /// </summary>
    private static void ConfigureApiVersionDeprecation(this IServiceCollection services)
    {
        services.AddOptions<ApiVersionDeprecationOptions>()
            .BindConfiguration(ConfigurationKeys.ApiVersionDeprecationOptions)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    /// Configures infrastructure services (database, repositories, migrations).
    /// </summary>
    private static void ConfigureInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Hosted services run in registration order at startup.
        // 1. Connection validation first (fail-fast if DB unreachable)
        // 2. Migrations second (only after connections are validated)
        services.AddHostedService<HostedServices.DatabaseConnectionValidationHostedService>();
        services.AddHostedService<HostedServices.DatabaseMigrationHostedService>();

        services.AddInfrastructure(configuration);
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
