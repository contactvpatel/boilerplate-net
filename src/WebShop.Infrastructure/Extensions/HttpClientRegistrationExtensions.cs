using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebShop.Core.Interfaces.Base;
using WebShop.Core.Interfaces.Services;
using WebShop.Infrastructure.Helpers;
using WebShop.Infrastructure.Services.External;
using WebShop.Infrastructure.Services.Internal;
using WebShop.Util;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering HTTP clients and external services (SSO, MIS, ASM).
/// Configures named HttpClient instances with Polly resilience policies.
/// </summary>
public static class HttpClientRegistrationExtensions
{
    private const string AsmServiceName = "AsmService";
    private const string MisServiceName = "MisService";
    private const string SsoServiceName = "SsoService";

    private const int DefaultTimeoutSeconds = 30;

    private static Action<HttpStandardResilienceOptions> CreateResilienceHandler(HttpResilienceOptions resilienceOptions, int timeoutSeconds)
    {
        return options =>
        {
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
            options.Retry.MaxRetryAttempts = resilienceOptions.MaxRetryAttempts;
            options.Retry.Delay = TimeSpan.FromSeconds(resilienceOptions.RetryBaseDelaySeconds);
            options.CircuitBreaker.FailureRatio = 0.1;
            options.CircuitBreaker.MinimumThroughput = resilienceOptions.CircuitBreakerMinimumThroughput;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(resilienceOptions.CircuitBreakerBreakDurationSeconds);
        };
    }

    /// <summary>
    /// Registers HTTP resilience options, configures named HttpClient instances for external services,
    /// and registers SsoService, MisService, AsmService, and UserContext.
    /// </summary>
    public static IServiceCollection AddInfrastructureHttpClients(this IServiceCollection services, IConfiguration configuration)
    {
        IConfigurationSection resilienceSection = configuration.GetSection(ConfigurationKeys.HttpResilienceOptions);
        services.AddOptions<HttpResilienceOptions>()
            .Bind(resilienceSection)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        HttpResilienceOptions resilienceOptions = new();
        resilienceSection.Bind(resilienceOptions);

        int ssoTimeout = GetServiceTimeout(configuration, SsoServiceName);
        int misTimeout = GetServiceTimeout(configuration, MisServiceName);
        int asmTimeout = GetServiceTimeout(configuration, AsmServiceName);

        RegisterHttpClient<SsoServiceOptions>(services, SsoServiceName, resilienceOptions, ssoTimeout,
            (client, options, sp) =>
            {
                ValidateAndSetBaseAddress(client, options.Url, SsoServiceName, sp);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
            });

        RegisterHttpClient<MisServiceOptions>(services, MisServiceName, resilienceOptions, misTimeout,
            (client, options, sp) =>
            {
                ValidateAndSetBaseAddress(client, options.Url, MisServiceName, sp);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                ConfigureAuthHeaders(client, HttpHeaderNames.MisAuthAppId, HttpHeaderNames.MisAuthAppSecret, options.Headers.AuthAppId, options.Headers.AuthAppSecret);
            });

        RegisterHttpClient<AsmServiceOptions>(services, AsmServiceName, resilienceOptions, asmTimeout,
            (client, options, sp) =>
            {
                ValidateAndSetBaseAddress(client, options.Url, AsmServiceName, sp);
                client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
                ConfigureAuthHeaders(client, HttpHeaderNames.AsmAuthAppId, HttpHeaderNames.AsmAuthAppSecret, options.Headers.AuthAppId, options.Headers.AuthAppSecret);
            });

        services.AddScoped<ISsoService, SsoService>();
        services.AddScoped<IMisService, MisService>();
        services.AddScoped<IAsmService, AsmService>();
        services.AddScoped<IUserContext, UserContext>();

        return services;
    }

    private static void RegisterHttpClient<TOptions>(
        IServiceCollection services,
        string clientName,
        HttpResilienceOptions resilienceOptions,
        int timeoutSeconds,
        Action<HttpClient, TOptions, IServiceProvider> configureClient)
        where TOptions : class
    {
        services.AddHttpClient(clientName, (serviceProvider, client) =>
        {
            TOptions options = serviceProvider.GetRequiredService<IOptions<TOptions>>().Value;
            configureClient(client, options, serviceProvider);
        })
            .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
            {
                HttpResilienceOptions options = serviceProvider.GetRequiredService<IOptions<HttpResilienceOptions>>().Value;
                return CreateHttpMessageHandler(options);
            })
            .AddStandardResilienceHandler(CreateResilienceHandler(resilienceOptions, timeoutSeconds));
    }

    private static int GetServiceTimeout(IConfiguration configuration, string sectionName)
    {
        int timeout = configuration.GetSection(sectionName).GetValue<int>("TimeoutSeconds");
        return timeout > 0 ? timeout : DefaultTimeoutSeconds;
    }

    private static void ConfigureAuthHeaders(HttpClient client, string appIdHeaderName, string appSecretHeaderName, string? appId, string? appSecret)
    {
        if (!string.IsNullOrWhiteSpace(appId))
        {
            client.DefaultRequestHeaders.Add(appIdHeaderName, appId);
        }
        if (!string.IsNullOrWhiteSpace(appSecret))
        {
            client.DefaultRequestHeaders.Add(appSecretHeaderName, appSecret);
        }
    }

    private static void ValidateAndSetBaseAddress(
        HttpClient client,
        string url,
        string serviceName,
        IServiceProvider serviceProvider)
    {
        if (!UrlValidator.IsValidExternalUrl(url, out Uri? uri))
        {
            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger("WebShop.Infrastructure.Extensions.HttpClientRegistrationExtensions");
            logger.LogError(
                "Invalid {ServiceName} service URL: {Url}. URL must be HTTPS and cannot be localhost or private IP.",
                serviceName, url);
            throw new InvalidOperationException(
                $"Invalid {serviceName} service URL: {url}. URL must be HTTPS and cannot be localhost or private IP.");
        }

        client.BaseAddress = uri;
    }

    private static SocketsHttpHandler CreateHttpMessageHandler(HttpResilienceOptions resilienceOptions)
    {
        return new SocketsHttpHandler
        {
            MaxConnectionsPerServer = resilienceOptions.MaxConnectionsPerServer,
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        };
    }
}
