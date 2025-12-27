using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using WebShop.Core.Interfaces;
using WebShop.Infrastructure.Helpers;
using WebShop.Infrastructure.Interfaces;
using WebShop.Infrastructure.Repositories;
using WebShop.Util.Models;

namespace WebShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment? environment = null)
    {
        ConfigureCaching(services, configuration);

        // Register Dapper connection factory (for Dapper repositories)
        services.AddSingleton<IDapperConnectionFactory, DapperConnectionFactory>();

        // Register Dapper transaction manager (scoped for request-based transactions)
        services.AddScoped<IDapperTransactionManager, DapperTransactionManager>();

        // Register repositories (all using Dapper)
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderPositionRepository, OrderPositionRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<IColorRepository, ColorRepository>();
        services.AddScoped<ISizeRepository, SizeRepository>();
        services.AddScoped<IStockRepository, StockRepository>();

        // Register infrastructure services
        ConfigureHttpClients(services, configuration);

        // Register HTTP resilience options
        services.Configure<HttpResilienceOptions>(configuration.GetSection("HttpResilienceOptions"));

        services.AddScoped<Core.Interfaces.Services.ISsoService, Services.External.SsoService>();
        services.AddScoped<Core.Interfaces.Services.IMisService, Services.External.MisService>();
        services.AddScoped<Core.Interfaces.Services.IAsmService, Services.External.AsmService>();
        services.AddScoped<Core.Interfaces.Base.IUserContext, Services.Internal.UserContext>();

        // Register cache options
        services.Configure<CacheOptions>(configuration.GetSection("CacheOptions"));

        // Register cache service (handles disabled state internally)
        services.AddScoped<Core.Interfaces.Base.ICacheService, Services.Internal.CacheService>();

        return services;
    }

    /// <summary>
    /// Configures named HttpClient instances for external services (SSO, MIS, ASM) with Polly resilience policies.
    /// </summary>
    private static void ConfigureHttpClients(IServiceCollection services, IConfiguration configuration)
    {
        // Load service configurations
        SsoServiceOptions ssoServiceOptions = new();
        configuration.GetSection("SsoService").Bind(ssoServiceOptions);

        MisServiceOptions misServiceOptions = new();
        configuration.GetSection("MisService").Bind(misServiceOptions);

        AsmServiceOptions asmServiceOptions = new();
        configuration.GetSection("AsmService").Bind(asmServiceOptions);

        // Load HTTP resilience configuration with defaults
        HttpResilienceOptions resilienceOptions = new();
        configuration.GetSection("HttpResilienceOptions").Bind(resilienceOptions);

        // Register all external service HttpClients using common configuration pattern
        RegisterHttpClient(services, "SsoService", ssoServiceOptions.Url, ssoServiceOptions.TimeoutSeconds,
            resilienceOptions, configureHeaders: null);

        RegisterHttpClient(services, "MisService", misServiceOptions.Url, misServiceOptions.TimeoutSeconds,
            resilienceOptions, client =>
            {
                // Thread-safe: Set service-level headers at HttpClient factory level
                if (!string.IsNullOrWhiteSpace(misServiceOptions.Headers.AuthAppId))
                {
                    client.DefaultRequestHeaders.Add("x-baps-auth-app-id", misServiceOptions.Headers.AuthAppId);
                }
                if (!string.IsNullOrWhiteSpace(misServiceOptions.Headers.AuthAppSecret))
                {
                    client.DefaultRequestHeaders.Add("x-baps-auth-app-secret", misServiceOptions.Headers.AuthAppSecret);
                }
            });

        RegisterHttpClient(services, "AsmService", asmServiceOptions.Url, asmServiceOptions.TimeoutSeconds,
            resilienceOptions, client =>
            {
                // Thread-safe: Set service-level headers at HttpClient factory level
                if (!string.IsNullOrWhiteSpace(asmServiceOptions.Headers.AuthAppId))
                {
                    client.DefaultRequestHeaders.Add("x-app-auth-id", asmServiceOptions.Headers.AuthAppId);
                }
                if (!string.IsNullOrWhiteSpace(asmServiceOptions.Headers.AuthAppSecret))
                {
                    client.DefaultRequestHeaders.Add("x-app-auth-secret", asmServiceOptions.Headers.AuthAppSecret);
                }
            });
    }

    /// <summary>
    /// Registers a named HttpClient with common configuration, resilience policies, and optional header configuration.
    /// Uses Microsoft.Extensions.Http.Resilience for modern resilience patterns (rate limiter, timeout, retry, circuit breaker).
    /// Follows DRY principle by centralizing common HttpClient setup logic.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="clientName">The name of the HttpClient.</param>
    /// <param name="baseUrl">The base URL for the service.</param>
    /// <param name="timeoutSeconds">The timeout in seconds.</param>
    /// <param name="resilienceOptions">The HTTP resilience configuration options.</param>
    /// <param name="configureHeaders">Optional action to configure service-specific headers.</param>
    private static void RegisterHttpClient(
        IServiceCollection services,
        string clientName,
        string baseUrl,
        int timeoutSeconds,
        HttpResilienceOptions resilienceOptions,
        Action<HttpClient>? configureHeaders = null)
    {
        services.AddHttpClient(clientName, (serviceProvider, client) =>
        {
            // Security: Validate URL before use to prevent SSRF attacks
            ValidateAndSetBaseAddress(client, baseUrl, clientName, serviceProvider);

            // Use configured timeout instead of hardcoded value
            client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            // Configure service-specific headers if provided
            configureHeaders?.Invoke(client);
        })
        .ConfigurePrimaryHttpMessageHandler(() => CreateHttpMessageHandler(resilienceOptions))
        .AddStandardResilienceHandler(options =>
        {
            // Configure standard resilience handler with custom options from HttpResilienceOptions
            // The standard handler includes (in order):
            // 1. Rate limiter (1000 permits, queue: 0) for outgoing requests
            // 2. Total timeout (30s default, using configured timeout)
            // 3. Retry (3 retries with exponential backoff)
            // 4. Circuit breaker (10% failure ratio, 100 min throughput, 30s sampling, 5s break)
            // 5. Attempt timeout (10s per attempt)

            // Configure total timeout
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

            // Configure retry strategy
            options.Retry.MaxRetryAttempts = resilienceOptions.MaxRetryAttempts;
            options.Retry.Delay = TimeSpan.FromSeconds(resilienceOptions.RetryBaseDelaySeconds);

            // Configure circuit breaker strategy
            options.CircuitBreaker.FailureRatio = 0.1; // 10% failure ratio
            options.CircuitBreaker.MinimumThroughput = resilienceOptions.CircuitBreakerMinimumThroughput;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(resilienceOptions.CircuitBreakerBreakDurationSeconds);
        });
    }

    /// <summary>
    /// Validates the URL and sets the base address on the HttpClient.
    /// Throws InvalidOperationException if URL is invalid.
    /// </summary>
    /// <param name="client">The HttpClient to configure.</param>
    /// <param name="url">The URL to validate and set.</param>
    /// <param name="serviceName">The name of the service (for error messages).</param>
    /// <param name="serviceProvider">The service provider to get logger.</param>
    /// <exception cref="InvalidOperationException">Thrown when URL is invalid.</exception>
    private static void ValidateAndSetBaseAddress(
        HttpClient client,
        string url,
        string serviceName,
        IServiceProvider serviceProvider)
    {
        if (!UrlValidator.IsValidExternalUrl(url, out Uri? uri))
        {
            ILoggerFactory loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
            ILogger logger = loggerFactory.CreateLogger("WebShop.Infrastructure.DependencyInjection");
            logger.LogError(
                "Invalid {ServiceName} service URL: {Url}. URL must be HTTPS and cannot be localhost or private IP.",
                serviceName, url);
            throw new InvalidOperationException(
                $"Invalid {serviceName} service URL: {url}. URL must be HTTPS and cannot be localhost or private IP.");
        }

        client.BaseAddress = uri;
    }

    /// <summary>
    /// Creates a configured SocketsHttpHandler with connection pooling settings.
    /// </summary>
    /// <param name="resilienceOptions">The HTTP resilience configuration options.</param>
    /// <returns>A configured SocketsHttpHandler instance.</returns>
    private static SocketsHttpHandler CreateHttpMessageHandler(HttpResilienceOptions resilienceOptions)
    {
        return new SocketsHttpHandler
        {
            MaxConnectionsPerServer = resilienceOptions.MaxConnectionsPerServer,
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            PooledConnectionLifetime = TimeSpan.FromMinutes(10)
        };
    }


    /// <summary>
    /// Configures HybridCache with optional distributed cache support.
    /// HybridCache provides both in-memory (primary) and distributed (secondary) caching
    /// with automatic stampede protection and optimal performance.
    /// </summary>
    private static void ConfigureCaching(IServiceCollection services, IConfiguration configuration)
    {
        CacheOptions cacheOptions = new();
        configuration.GetSection("CacheOptions").Bind(cacheOptions);

        // Configure HybridCache only if enabled (otherwise it won't be registered and CacheService will handle it)
        if (!cacheOptions.Enabled)
        {
            return;
        }

        // Configure HybridCache with default options
        services.AddHybridCache(options =>
        {
            // Set default expiration times
            TimeSpan? defaultExpiration = cacheOptions.GetDefaultExpiration();
            TimeSpan? defaultLocalExpiration = cacheOptions.GetDefaultLocalExpiration();

            if (defaultExpiration.HasValue || defaultLocalExpiration.HasValue)
            {
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = defaultExpiration ?? TimeSpan.FromMinutes(10),
                    LocalCacheExpiration = defaultLocalExpiration ?? defaultExpiration ?? TimeSpan.FromMinutes(5)
                };
            }
            else
            {
                // Default to 10 minutes expiration, 5 minutes local expiration
                options.DefaultEntryOptions = new HybridCacheEntryOptions
                {
                    Expiration = TimeSpan.FromMinutes(10),
                    LocalCacheExpiration = TimeSpan.FromMinutes(5)
                };
            }

            // Configure limits
            if (cacheOptions.MaximumPayloadBytes.HasValue)
            {
                options.MaximumPayloadBytes = cacheOptions.MaximumPayloadBytes.Value;
            }

            if (cacheOptions.MaximumKeyLength.HasValue)
            {
                options.MaximumKeyLength = cacheOptions.MaximumKeyLength.Value;
            }
        });

        if (!string.IsNullOrWhiteSpace(cacheOptions.RedisConnectionString))
        {
            // Register Redis distributed cache with advanced configuration
            // HybridCache will automatically use the registered IDistributedCache
            services.AddStackExchangeRedisCache(options =>
            {
                // Parse connection string to ConfigurationOptions for more control
                options.ConfigurationOptions = ConfigurationOptions.Parse(cacheOptions.RedisConnectionString, ignoreUnknown: true);

                // Set instance name for key prefixing (useful for multi-tenant scenarios)
                if (!string.IsNullOrWhiteSpace(cacheOptions.RedisInstanceName))
                {
                    options.InstanceName = cacheOptions.RedisInstanceName;
                }

                // Configure SSL certificate validation
                options.ConfigurationOptions.CertificateValidation += ValidateServerCertificate;
            });
        }
        // If no distributed cache is configured, HybridCache will use only in-memory caching
        // This is perfectly fine for single-server scenarios or development
    }

    /// <summary>
    /// Validates Redis server certificate.
    /// Allows RemoteCertificateNameMismatch for development scenarios (e.g., Cloud Redis Cache).
    /// </summary>
    /// <param name="sender">The sender object.</param>
    /// <param name="certificate">The certificate to validate.</param>
    /// <param name="chain">The certificate chain.</param>
    /// <param name="sslPolicyErrors">SSL policy errors.</param>
    /// <returns>True if certificate is valid, false otherwise.</returns>
    private static bool ValidateServerCertificate(
        object _,
        X509Certificate? _1,
        X509Chain? _2,
        System.Net.Security.SslPolicyErrors sslPolicyErrors)
    {
        // Allow RemoteCertificateNameMismatch (common with Cloud Redis Cache)
        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch)
        {
            return true;
        }

        // Log other certificate errors for debugging
        if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
        {
            Console.WriteLine("Redis certificate validation error: {0}", sslPolicyErrors);
        }

        return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
    }
}

