using WebShop.Util.Models;

namespace WebShop.Api.Extensions.Core;

/// <summary>
/// Extension methods for configuring application settings and validation.
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Configures application settings and configuration bindings with validation.
    /// </summary>
    public static void ConfigureApplicationSettings(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure AppSettings with validation
        services.AddOptions<AppSettingModel>()
            .Bind(configuration.GetSection("AppSettings"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure SsoServiceOptions with validation
        services.AddOptions<SsoServiceOptions>()
            .Bind(configuration.GetSection("SsoService"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure MisServiceOptions with validation
        services.AddOptions<MisServiceOptions>()
            .Bind(configuration.GetSection("MisService"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Configure AsmServiceOptions with validation
        services.AddOptions<AsmServiceOptions>()
            .Bind(configuration.GetSection("AsmService"))
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    /// <summary>
    /// Initializes configuration providers with explicit ordering.
    /// Configuration providers are loaded in this order (later providers override earlier ones):
    /// 1. appsettings.json - Base configuration
    /// 2. appsettings.{Environment}.json - Environment-specific overrides
    /// 3. Environment variables - Highest priority (for secrets and runtime overrides)
    /// 
    /// Note: WebApplication.CreateBuilder already sets up default configuration providers.
    /// We clear and rebuild them to ensure explicit ordering and clarity.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    public static void InitializeConfigurationProviders(this WebApplicationBuilder builder)
    {
        string environmentName = builder.Environment.EnvironmentName;

        // Clear existing sources and rebuild in explicit order
        builder.Configuration.Sources.Clear();

        builder.Configuration
            // 1. Base configuration - required, loaded first
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            // 2. Environment-specific configuration - optional, overrides base
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true, reloadOnChange: true)
            // 3. Environment variables - highest priority, overrides all JSON files
            // Uses double underscore (__) for nested keys (e.g., AppSettings__Environment)
            // Example: AppSettings__Environment=Production
            .AddEnvironmentVariables();
    }
}
