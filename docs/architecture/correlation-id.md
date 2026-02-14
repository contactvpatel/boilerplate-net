# Correlation ID Approach

This document explains how the WebShop API handles request correlation and why we use OpenTelemetry TraceId as the correlation ID.

[← Back to README](../README.md)

## Table of Contents

- [Overview](#overview)
- [Our Approach: TraceId as CorrelationId](#our-approach-traceid-as-correlationid)
- [Why Not a Separate Correlation ID?](#why-not-a-separate-correlation-id)
- [Implementation](#implementation)
- [Response Headers](#response-headers)
- [Accessing the Correlation ID in Code](#accessing-the-correlation-id-in-code)
- [Related Documentation](#related-documentation)

---

## Overview

A **correlation ID** is a unique identifier that ties together all logs, traces, and operations for a single request. It enables support teams and developers to trace a request across services and logs when debugging issues.

Modern cloud-native systems typically use **distributed tracing** (OpenTelemetry, W3C Trace Context) for this purpose. The TraceId from a trace span serves as the correlation identifier.

---

## Our Approach: TraceId as CorrelationId

**Best practice:** Use OpenTelemetry TraceId as the correlation ID. Do not generate a separate identifier.

| Aspect | Our Approach |
|--------|--------------|
| **Source** | `Activity.Current?.TraceId` (from OpenTelemetry) |
| **Response header** | `X-Correlation-Id` (exposes TraceId for clients) |
| **Logs** | TraceId is automatically included via OpenTelemetry/Serilog enrichment |
| **Distributed tracing** | Same ID used across spans, logs, and metrics |

**Benefits:**

1. **Single source of truth** – One ID for tracing, logging, and client-facing correlation
2. **No duplication** – No separate GUID generation or client header forwarding
3. **Industry standard** – Aligns with W3C Trace Context and OpenTelemetry
4. **Cloud-native** – Matches how Azure, AWS, and Kubernetes observability work

---

## Why Not a Separate Correlation ID?

| Alternative | Why We Don't Use It |
|-------------|---------------------|
| **Generate new GUID per request** | Creates a second ID; logs and traces would use TraceId, clients would get a different value. Harder to correlate. |
| **Accept X-Correlation-Id from client** | Useful for client-initiated correlation, but adds complexity. Most clients can use TraceId from our response. |
| **Use ErrorId only** | ErrorId is per-exception, not per-request. TraceId covers the full request lifecycle. |

We keep the implementation simple: TraceId is the correlation ID. The `CorrelationIdMiddleware` only exposes it as `X-Correlation-Id` for clients that expect that header.

---

## Implementation

**Location:** `src/WebShop.Api/Middleware/CorrelationIdMiddleware.cs`

The middleware:

1. Reads `Activity.Current?.TraceId` (created by OpenTelemetry when the request is received)
2. Stores it in `HttpContext.Items["CorrelationId"]` for use within the request
3. Adds `X-Correlation-Id: {TraceId}` to the response headers

If TraceId is not available (e.g., tracing not yet active), the header is not added. OpenTelemetry's `traceparent` header will still be present when tracing is configured.

**Pipeline order:** The middleware runs early (after HTTPS enforcement) so the correlation ID is available for the entire request.

---

## Response Headers

Every API response includes (when tracing is active):

| Header | Value | Purpose |
|--------|-------|---------|
| `X-Correlation-Id` | TraceId (e.g., `0af7651916cd43dd8448eb211c80319c`) | Client-facing correlation for support and debugging |
| `traceparent` | W3C Trace Context | Standard header for distributed tracing (from OpenTelemetry) |

Clients can use `X-Correlation-Id` when contacting support: "Request failed, correlation ID: 0af7651916cd43dd8448eb211c80319c."

---

## Accessing the Correlation ID in Code

Within a request, you can read the correlation ID from `HttpContext`:

```csharp
string? correlationId = context.Items[CorrelationIdMiddleware.CorrelationIdItemKey] as string;
```

Or from `Activity.Current`:

```csharp
string? traceId = Activity.Current?.TraceId.ToString();
```

For logging, Serilog and OpenTelemetry automatically enrich logs with TraceId when configured. No manual correlation ID logging is required.

---

## Related Documentation

- [OpenTelemetry Integration](opentelemetry-integration.md) – Distributed tracing setup and configuration
- [Exception Handling](exception-handling.md) – ErrorId in error responses (per-exception; TraceId is per-request)
- [Logging Strategy Recommendations](../standards/logging-strategy-recommendations.md) – Structured logging and trace correlation
