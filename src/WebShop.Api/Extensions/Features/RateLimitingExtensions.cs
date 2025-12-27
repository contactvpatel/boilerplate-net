using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using WebShop.Util.Models;

namespace WebShop.Api.Extensions.Features;

/// <summary>
/// Extension methods for configuring ASP.NET Core Rate Limiting.
/// Follows Microsoft guidelines and .NET 10 best practices.
/// </summary>
public static class RateLimitingExtensions
{
    /// <summary>
    /// Configures rate limiting services based on configuration.
    /// </summary>
    public static void ConfigureRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        RateLimitingOptions options = new();
        configuration.GetSection("RateLimitingOptions").Bind(options);

        if (!options.Enabled)
        {
            return;
        }

        services.AddRateLimiter(rateLimiterOptions =>
        {
            // This limits INCOMING requests to API
            // Global rate limiter - applies to all requests unless overridden
            // Uses partition key (user ID or IP) for per-user/IP rate limiting
            rateLimiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetPartitionKey(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.GlobalPolicy.PermitLimit,
                        Window = TimeSpan.FromMinutes(options.GlobalPolicy.WindowMinutes),
                        QueueLimit = options.GlobalPolicy.QueueLimit,
                        AutoReplenishment = true
                    }));

            // Strict policy for sensitive endpoints (authentication, write operations)
            rateLimiterOptions.AddFixedWindowLimiter("strict", limiterOptions =>
            {
                limiterOptions.PermitLimit = options.StrictPolicy.PermitLimit;
                limiterOptions.Window = TimeSpan.FromMinutes(options.StrictPolicy.WindowMinutes);
                limiterOptions.QueueLimit = options.StrictPolicy.QueueLimit;
                limiterOptions.AutoReplenishment = true;
            });

            // Permissive policy for read-only endpoints
            rateLimiterOptions.AddFixedWindowLimiter("permissive", limiterOptions =>
            {
                limiterOptions.PermitLimit = options.PermissivePolicy.PermitLimit;
                limiterOptions.Window = TimeSpan.FromMinutes(options.PermissivePolicy.WindowMinutes);
                limiterOptions.QueueLimit = options.PermissivePolicy.QueueLimit;
                limiterOptions.AutoReplenishment = true;
            });

            // Rejection response when rate limit is exceeded
            rateLimiterOptions.OnRejected = async (context, cancellationToken) =>
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.HttpContext.Response.ContentType = "application/json";

                var response = new
                {
                    succeeded = false,
                    data = (object?)null,
                    message = "Rate limit exceeded. Please try again later.",
                    errors = new[]
                    {
                        new
                        {
                            errorId = Guid.NewGuid().ToString(),
                            statusCode = (short)HttpStatusCode.TooManyRequests,
                            message = "Too many requests. Please retry after the specified time."
                        }
                    }
                };

                // Add rate limit headers (RFC 6585)
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
                {
                    context.HttpContext.Response.Headers.Append("Retry-After", retryAfter.TotalSeconds.ToString());
                }

                await context.HttpContext.Response.WriteAsJsonAsync(response, cancellationToken);
            };
        });
    }

    /// <summary>
    /// Gets the partition key for rate limiting (user ID, IP address, or anonymous).
    /// </summary>
    private static string GetPartitionKey(HttpContext context)
    {
        // Priority 1: Authenticated user ID (most accurate)
        if (context.User?.Identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(context.User.Identity.Name))
        {
            return $"user:{context.User.Identity.Name}";
        }

        // Priority 2: User ID from HttpContext.Items (set by JWT filter)
        if (context.Items.TryGetValue("UserId", out object? userId) && userId != null)
        {
            return $"user:{userId}";
        }

        // Priority 3: IP address (for anonymous users)
        string? ipAddress = context.Connection.RemoteIpAddress?.ToString();
        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            return $"ip:{ipAddress}";
        }

        // Fallback: anonymous
        return "anonymous";
    }

    /// <summary>
    /// Creates a rate limiter instance based on the algorithm specified in the policy.
    /// </summary>
    private static RateLimiter CreateRateLimiter(RateLimitPolicy policy)
    {
        return policy.Algorithm.ToLowerInvariant() switch
        {
            "fixedwindow" => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromMinutes(policy.WindowMinutes),
                QueueLimit = policy.QueueLimit,
                AutoReplenishment = true
            }),

            "slidingwindow" => new SlidingWindowRateLimiter(new SlidingWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromMinutes(policy.WindowMinutes),
                SegmentsPerWindow = 10, // Default: 10 segments for smooth sliding
                QueueLimit = policy.QueueLimit,
                AutoReplenishment = true
            }),

            "tokenbucket" => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
            {
                TokenLimit = policy.TokenLimit,
                ReplenishmentPeriod = TimeSpan.FromSeconds(policy.ReplenishmentPeriodSeconds),
                TokensPerPeriod = policy.TokensPerPeriod,
                QueueLimit = policy.QueueLimit,
                AutoReplenishment = true
            }),

            "concurrency" => new ConcurrencyLimiter(new ConcurrencyLimiterOptions
            {
                PermitLimit = policy.PermitLimitConcurrency,
                QueueLimit = policy.QueueLimitConcurrency
            }),

            _ => new FixedWindowRateLimiter(new FixedWindowRateLimiterOptions
            {
                PermitLimit = policy.PermitLimit,
                Window = TimeSpan.FromMinutes(policy.WindowMinutes),
                QueueLimit = policy.QueueLimit,
                AutoReplenishment = true
            })
        };
    }
}

