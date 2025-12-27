using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WebShop.Api.Helpers;

/// <summary>
/// Handles writing health check responses in various formats.
/// </summary>
public class HealthCheckResponseWriter
{
    private readonly IConfiguration _configuration;

    public HealthCheckResponseWriter(IConfiguration configuration)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    /// <summary>
    /// Writes an enhanced health check response with detailed information about each check.
    /// </summary>
    public async Task WriteEnhancedAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        string? applicationVersion = _configuration.GetValue<string>("AppSettings:ApplicationVersion");

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            version = applicationVersion,
            totalDuration = $"{report.TotalDuration.TotalMilliseconds:F2}ms",
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = $"{entry.Value.Duration.TotalMilliseconds:F2}ms",
                tags = entry.Value.Tags,
                exception = entry.Value.Exception?.Message,
                data = entry.Value.Data
            })
        };

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }

    /// <summary>
    /// Writes a detailed health check response with comprehensive information including exception details.
    /// </summary>
    public async Task WriteDetailedAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";

        string? applicationVersion = _configuration.GetValue<string>("AppSettings:ApplicationVersion");

        var response = new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            version = applicationVersion,
            totalDuration = $"{report.TotalDuration.TotalMilliseconds:F2}ms",
            totalDurationSeconds = report.TotalDuration.TotalSeconds,
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                duration = $"{entry.Value.Duration.TotalMilliseconds:F2}ms",
                durationSeconds = entry.Value.Duration.TotalSeconds,
                tags = entry.Value.Tags,
                exception = entry.Value.Exception != null ? new
                {
                    message = entry.Value.Exception.Message,
                    type = entry.Value.Exception.GetType().Name,
                    stackTrace = entry.Value.Exception.StackTrace
                } : null,
                data = entry.Value.Data,
                isHealthy = entry.Value.Status == HealthStatus.Healthy,
                isDegraded = entry.Value.Status == HealthStatus.Degraded,
                isUnhealthy = entry.Value.Status == HealthStatus.Unhealthy
            }),
            summary = new
            {
                total = report.Entries.Count,
                healthy = report.Entries.Count(e => e.Value.Status == HealthStatus.Healthy),
                degraded = report.Entries.Count(e => e.Value.Status == HealthStatus.Degraded),
                unhealthy = report.Entries.Count(e => e.Value.Status == HealthStatus.Unhealthy)
            }
        };

        JsonSerializerOptions options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

