using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using WebShop.Core.Interfaces.Base;
using WebShop.Infrastructure.Services.Internal;
using WebShop.Util;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering caching infrastructure (HybridCache, Redis).
/// HybridCache provides both in-memory (primary) and distributed (secondary) caching
/// with automatic stampede protection and optimal performance.
/// </summary>
public static class CachingExtensions
{
    private const int DefaultExpirationMinutes = 10;
    private const int DefaultLocalExpirationMinutes = 5;

    /// <summary>
    /// Configures HybridCache with optional distributed cache support.
    /// Registers CacheOptions and CacheService. If caching is disabled, HybridCache
    /// is not registered and CacheService handles the disabled state internally.
    /// </summary>
    public static IServiceCollection AddInfrastructureCaching(this IServiceCollection services, IConfiguration configuration)
    {
        CacheOptions cacheOptions = new();
        configuration.GetSection(ConfigurationKeys.CacheOptions).Bind(cacheOptions);

        if (cacheOptions.Enabled)
        {
            ConfigureHybridCache(services, cacheOptions);
        }

        services.AddOptions<CacheOptions>()
            .Bind(configuration.GetSection(ConfigurationKeys.CacheOptions))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddScoped<ICacheService, CacheService>();

        return services;
    }

    private static void ConfigureHybridCache(IServiceCollection services, CacheOptions cacheOptions)
    {
        services.AddHybridCache(options =>
        {
            TimeSpan? defaultExpiration = cacheOptions.GetDefaultExpiration();
            TimeSpan? defaultLocalExpiration = cacheOptions.GetDefaultLocalExpiration();

            options.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = defaultExpiration ?? TimeSpan.FromMinutes(DefaultExpirationMinutes),
                LocalCacheExpiration = defaultLocalExpiration ?? defaultExpiration ?? TimeSpan.FromMinutes(DefaultLocalExpirationMinutes)
            };

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
            services.AddStackExchangeRedisCache(redisOptions =>
            {
                redisOptions.ConfigurationOptions = ConfigurationOptions.Parse(cacheOptions.RedisConnectionString, ignoreUnknown: true);

                if (!string.IsNullOrWhiteSpace(cacheOptions.RedisInstanceName))
                {
                    redisOptions.InstanceName = cacheOptions.RedisInstanceName;
                }

                redisOptions.ConfigurationOptions.CertificateValidation += ValidateServerCertificate;
            });
        }
    }

    /// <summary>
    /// Validates Redis server certificate.
    /// Allows RemoteCertificateNameMismatch for development scenarios (e.g., Cloud Redis Cache).
    /// </summary>
    private static bool ValidateServerCertificate(
        object _,
        X509Certificate? _1,
        X509Chain? _2,
        System.Net.Security.SslPolicyErrors sslPolicyErrors)
    {
        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.RemoteCertificateNameMismatch)
        {
            return true;
        }

        if (sslPolicyErrors != System.Net.Security.SslPolicyErrors.None)
        {
            Trace.TraceWarning("Redis certificate validation error: {0}", sslPolicyErrors);
        }

        return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
    }
}
