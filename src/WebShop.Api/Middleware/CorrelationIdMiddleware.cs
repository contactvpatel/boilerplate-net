using System.Diagnostics;

namespace WebShop.Api.Middleware;

/// <summary>
/// Exposes OpenTelemetry TraceId as X-Correlation-Id in response headers.
/// Best practice: use TraceId as the correlation ID—no separate ID generation, aligns with distributed tracing.
/// </summary>
public class CorrelationIdMiddleware(RequestDelegate next)
{
    /// <summary>
    /// HTTP header name for correlation ID (response).
    /// </summary>
    public const string CorrelationIdHeader = "X-Correlation-Id";

    /// <summary>
    /// HttpContext.Items key for storing the correlation ID.
    /// </summary>
    public const string CorrelationIdItemKey = "CorrelationId";

    /// <summary>
    /// Invokes the middleware to set correlation ID from TraceId and add to response headers.
    /// </summary>
    public async Task InvokeAsync(HttpContext context)
    {
        string? traceId = Activity.Current?.TraceId.ToString();
        if (!string.IsNullOrWhiteSpace(traceId))
        {
            context.Items[CorrelationIdItemKey] = traceId;

            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
                {
                    context.Response.Headers.Append(CorrelationIdHeader, traceId);
                }

                return Task.CompletedTask;
            });
        }

        await next(context);
    }
}
