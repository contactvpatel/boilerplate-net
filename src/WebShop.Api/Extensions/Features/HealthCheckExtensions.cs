using System.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebShop.Api.Helpers;
using WebShop.Infrastructure.Helpers;
using WebShop.Infrastructure.Interfaces;

namespace WebShop.Api.Extensions.Features;

/// <summary>
/// Health check for Dapper database connections.
/// Uses shared DatabaseConnectionValidator for async validation (no sync-over-async).
/// </summary>
public class DapperHealthCheck(IDapperConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using IDbConnection readConnection = connectionFactory.CreateReadConnection();
            await DatabaseConnectionValidator.ValidateAsync(readConnection, cancellationToken).ConfigureAwait(false);

            using IDbConnection writeConnection = connectionFactory.CreateWriteConnection();
            await DatabaseConnectionValidator.ValidateAsync(writeConnection, cancellationToken).ConfigureAwait(false);

            return HealthCheckResult.Healthy("Database connections are healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database connections are unhealthy", ex);
        }
    }
}

/// <summary>
/// Extension methods for configuring health checks.
/// </summary>
public static class HealthCheckExtensions
{
    private const string HealthCheckSelfName = "self";
    private const string HealthCheckDbReadName = "database-read";
    private const string HealthCheckDbWriteName = "database-write";
    private const string HealthCheckTagDb = "db";
    private const string HealthCheckTagRead = "read";
    private const string HealthCheckTagWrite = "write";
    private const string HealthCheckTagReady = "ready";

    /// <summary>
    /// Configures health checks for the application.
    /// </summary>
    public static void ConfigureHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck(HealthCheckSelfName, () => HealthCheckResult.Healthy("API is healthy"))
            .AddCheck<DapperHealthCheck>(HealthCheckDbReadName, tags: [HealthCheckTagDb, HealthCheckTagRead, HealthCheckTagReady])
            .AddCheck<DapperHealthCheck>(HealthCheckDbWriteName, tags: [HealthCheckTagDb, HealthCheckTagWrite, HealthCheckTagReady]);
    }

    /// <summary>
    /// Configures health check endpoints.
    /// </summary>
    public static void ConfigureHealthCheckEndpoints(this WebApplication app)
    {
        HealthCheckResponseWriter responseWriter = new(app.Configuration, app.Environment);

        // Standard health check endpoint with enhanced JSON response
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = responseWriter.WriteEnhancedAsync
        });

        // Detailed health check endpoint with full information
        app.MapHealthChecks("/health/detailed", new HealthCheckOptions
        {
            ResponseWriter = responseWriter.WriteDetailedAsync,
            Predicate = _ => true // Include all checks
        });

        // Readiness probe with enhanced JSON
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTagReady),
            ResponseWriter = responseWriter.WriteEnhancedAsync
        });

        // Liveness probe (minimal check)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false, // No checks for liveness
            ResponseWriter = responseWriter.WriteEnhancedAsync
        });

        // Database health check with enhanced JSON
        app.MapHealthChecks("/health/db", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(HealthCheckTagDb),
            ResponseWriter = responseWriter.WriteEnhancedAsync
        });
    }
}
