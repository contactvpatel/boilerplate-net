using System.Data;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using WebShop.Api.Helpers;
using WebShop.Infrastructure.Helpers;
using WebShop.Infrastructure.Interfaces;

namespace WebShop.Api.Extensions.Features;

/// <summary>
/// Health check for Dapper database connections.
/// </summary>
public class DapperHealthCheck : IHealthCheck
{
    private readonly IDapperConnectionFactory _connectionFactory;

    public DapperHealthCheck(IDapperConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try both read and write connections
            using IDbConnection readConnection = _connectionFactory.CreateReadConnection();
            readConnection.Open();
            using IDbCommand readCommand = readConnection.CreateCommand();
            readCommand.CommandText = "SELECT 1";
            await Task.Run(() => readCommand.ExecuteScalar(), cancellationToken);

            using IDbConnection writeConnection = _connectionFactory.CreateWriteConnection();
            writeConnection.Open();
            using IDbCommand writeCommand = writeConnection.CreateCommand();
            writeCommand.CommandText = "SELECT 1";
            await Task.Run(() => writeCommand.ExecuteScalar(), cancellationToken);

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
            .AddCheck<DapperHealthCheck>(HealthCheckDbReadName, tags: [HealthCheckTagDb, HealthCheckTagRead])
            .AddCheck<DapperHealthCheck>(HealthCheckDbWriteName, tags: [HealthCheckTagDb, HealthCheckTagWrite]);
    }

    /// <summary>
    /// Configures health check endpoints.
    /// </summary>
    public static void ConfigureHealthCheckEndpoints(this WebApplication app)
    {
        HealthCheckResponseWriter responseWriter = new(app.Configuration);

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
