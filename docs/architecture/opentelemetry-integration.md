# OpenTelemetry Integration Guide

This guide explains how to consume and work with OpenTelemetry observability features in the WebShop .NET API, including distributed tracing, metrics collection, and structured logging.

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Installation](#installation)
- [Configuration](#configuration)
- [Log Output Examples](#log-output-examples)
- [Features](#features)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

---

## Overview

OpenTelemetry provides comprehensive observability for the WebShop API through automatic instrumentation and structured data collection. As a consumer, you can:

- **View Distributed Traces**: Follow requests across services and database calls
- **Monitor Metrics**: Track API performance, database usage, and system health
- **Analyze Logs**: Structured JSON logs with trace correlation and business context
- **Debug Issues**: Correlated logs, traces, and metrics for faster troubleshooting

**Key Benefits for Developers:**

- **Request Correlation**: Trace IDs link related logs, traces, and metrics
- **Automatic Instrumentation**: No code changes needed for basic observability
- **Production-Ready**: Enterprise-grade security, performance, and reliability
- **Vendor Neutral**: Works with any observability platform (Grafana, Datadog, etc.)

---

## Installation

### Step 1: Create a Personal Access Token (PAT)

Since the `OpenTelemetry.NET` package is hosted on GitHub Packages, you need to authenticate using a Personal Access Token (PAT).

1. Go to [GitHub Settings > Developer settings > Personal access tokens > Tokens (classic)](https://github.com/settings/tokens).
2. Click **Generate new token**.
3. Select the **`read:packages`** scope.
4. Generate the token and copy it (you won't be able to see it again).

### Step 2: Authenticate with GitHub Packages

Run the following command to add the GitHub Packages source to your NuGet configuration. Replace `YOUR_GITHUB_USERNAME` and `YOUR_GITHUB_PAT` with your details.

```bash
dotnet nuget add source https://nuget.pkg.github.com/baps-apps/index.json \
  --name github \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT
```

### Verification: Check Source Registration

Run the following command to verify the source was added correctly:

```bash
dotnet nuget list source
```

You should see output similar to:

```text
Registered Sources:
  1.  nuget.org [Enabled]
      https://api.nuget.org/v3/index.json
  2.  github [Enabled]
      https://nuget.pkg.github.com/baps-apps/index.json
```

If it’s listed and enabled, the source is registered.

### Step 3: Install Package

Install the `OpenTelemetry.NET` package into your API project. This package includes all necessary dependencies (Serilog, OpenTelemetry SDK, instrumentation libraries).

```bash
dotnet add package OpenTelemetry.NET --source github
```

---

## Configuration

OpenTelemetry is configured via `appsettings.json`. The infrastructure team manages the core configuration, but you can influence observability behavior through application settings.

### Basic Configuration (Managed by Infra)

```json
{
  "AppSettings": {
    "ApplicationName": "webshop-api-dev",
    "Environment": "Dev"
  },
  "OpenTelemetryOptions": {
    "EnableTracing": true,
    "EnableMetrics": true,
    "EnableLogging": true,
    "CollectorUrl": "http://localhost:4317"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

### Configuration Options Reference

| Setting | Purpose | Default | Notes |
|---------|---------|---------|-------|
| `EnableTracing` | Distributed tracing | `true` | Follows requests across services |
| `EnableMetrics` | Performance metrics | `true` | API response times, DB usage |
| `EnableLogging` | Structured logging | `true` | JSON logs with correlation |
| `CollectorUrl` | OTLP endpoint | Required | Managed by infrastructure |
| `TraceSamplingRate` | % of traces to collect | `0.1` (prod), `1.0` (dev) | Reduces overhead |

**Note:** Core OpenTelemetry configuration is managed by the infrastructure team. Contact them for changes to sampling rates, collector endpoints, or advanced settings.

---

## Log Output Examples

All logs follow the OpenTelemetry LogRecord data model with structured JSON output. Here are examples of how WebShop API logs appear:

### INFO Level - Successful Customer Creation

```json
{
  "Timestamp": "2026-01-08T14:30:25.500Z",
  "SeverityText": "INFO",
  "SeverityNumber": 9,
  "Body": "Customer created successfully",
  "TraceId": "a1b2c3d4e5f678901234567890abcdef",
  "SpanId": "fedcba0987654321",
  "TraceFlags": "01",
  "Resource": {
    "service.name": "webshop-api",
    "service.version": "1.0.0",
    "deployment.environment": "production",
    "k8s.pod.name": "webshop-api-7f8d9c4b2-abc123"
  },
  "Attributes": {
    "customer.id": 12345,
    "customer.email": "john.doe@[REDACTED]",
    "http.request.method": "POST",
    "http.route": "/api/v1/customers",
    "http.response.status_code": 201,
    "operation.duration_ms": 450
  }
}
```

### WARN Level - Database Connection Issue

```json
{
  "Timestamp": "2026-01-08T14:32:10.200Z",
  "SeverityText": "WARN",
  "SeverityNumber": 13,
  "Body": "Database connection pool nearing capacity",
  "TraceId": "b2c3d4e5f678901234567890abcdef12",
  "SpanId": "abcd1234efgh5678",
  "TraceFlags": "01",
  "Resource": {
    "service.name": "webshop-api",
    "service.version": "1.0.0",
    "deployment.environment": "production"
  },
  "Attributes": {
    "db.system": "postgresql",
    "db.connection.pool.active": 8,
    "db.connection.pool.idle": 2,
    "db.connection.pool.max": 10,
    "warning.type": "ConnectionPoolUsage"
  }
}
```

### ERROR Level - Order Processing Failure

```json
{
  "Timestamp": "2026-01-08T14:35:45.800Z",
  "SeverityText": "ERROR",
  "SeverityNumber": 17,
  "Body": "Order processing failed: insufficient inventory",
  "TraceId": "c3d4e5f678901234567890abcdef1234",
  "SpanId": "bcde5678fghi9012",
  "TraceFlags": "01",
  "Resource": {
    "service.name": "webshop-api",
    "service.version": "1.0.0",
    "deployment.environment": "production"
  },
  "Attributes": {
    "customer.id": 12345,
    "order.id": "ORD-2024-00123",
    "product.id": 67890,
    "product.requested_quantity": 5,
    "product.available_quantity": 2,
    "http.request.method": "POST",
    "http.route": "/api/v1/orders",
    "http.response.status_code": 400,
    "error.type": "BusinessLogicError",
    "error.code": "INSUFFICIENT_INVENTORY"
  }
}
```

### TRACE Level - Detailed Request Processing

```json
{
  "Timestamp": "2026-01-08T14:36:12.050Z",
  "SeverityText": "TRACE",
  "SeverityNumber": 1,
  "Body": "Processing product search request",
  "TraceId": "d4e5f678901234567890abcdef123456",
  "SpanId": "cdef7890ghij1234",
  "TraceFlags": "01",
  "Resource": {
    "service.name": "webshop-api",
    "service.version": "1.0.0",
    "deployment.environment": "development"
  },
  "Attributes": {
    "http.request.method": "GET",
    "http.route": "/api/v1/products",
    "url.query": "category=electronics&brand=samsung&minPrice=100",
    "operation.type": "search",
    "search.filters_applied": 3,
    "code.function.name": "ProductController.SearchProducts",
    "code.line.number": 45
  }
}
```

---

## Features

### Automatic Instrumentation

OpenTelemetry automatically instruments your WebShop API without code changes:

**HTTP Requests:**

- All API endpoints (`/api/v1/*`) are traced
- Request/response headers, status codes, and timing
- Sensitive data (Authorization, cookies) automatically masked

**Database Operations:**

- Dapper queries in repositories are traced
- Connection pooling metrics
- SQL execution timing (without exposing sensitive data)

**External Service Calls:**

- HTTP calls to SSO, MIS, and ASM services
- Resilience pattern execution (retries, circuit breakers)
- Service response times and error rates

**System Metrics:**

- .NET runtime performance (GC, threads)
- Process metrics (CPU, memory)
- Database connection usage

### Business Context Enrichment

Logs and traces include WebShop-specific business attributes:

```json
{
  "Attributes": {
    "customer.id": 12345,
    "order.id": "ORD-2024-00123",
    "product.id": 67890,
    "operation.type": "order_processing",
    "business.impact": "high"
  }
}
```

### Security Features

**Automatic Data Protection:**

- Email addresses: `user@domain.com` → `user@[REDACTED]`
- Authorization headers: Always `[REDACTED]`
- Credit card patterns: Automatically masked
- Query parameters with sensitive keywords: Masked

**DoS Protection:**

- Request size limits
- Attribute value length limits
- Safe error handling

---

## Best Practices

### For Development

**1. Use Appropriate Log Levels**

```csharp
// ✅ Good - Use structured logging with context
_logger.LogInformation(
    "Customer {CustomerId} placed order {OrderId}",
    customerId, orderId);

// ❌ Bad - String interpolation loses structure
_logger.LogInformation($"Customer {customerId} placed order {orderId}");
```

**2. Add Business Context**

```csharp
using var activity = Activity.Current;
activity?.SetTag("customer.id", customerId);
activity?.SetTag("order.total", orderTotal);
activity?.SetTag("operation.type", "checkout");
```

**3. Handle Sensitive Data**

```csharp
// ✅ Good - Framework automatically masks
_logger.LogInformation("Processing payment for customer {CustomerId}", customerId);

// ❌ Bad - Never log sensitive data directly
_logger.LogInformation($"Processing payment: {cardNumber}");
```

### For Debugging

**1. Correlate Issues Across Components**

- Use TraceId to follow requests from API → Database → External Services
- Check span timing to identify bottlenecks
- Review error attributes for failure context

**2. Monitor Key Metrics**

- API response times (P95 should be <500ms)
- Database connection pool usage (<80% sustained)
- Error rates by endpoint (<1% for critical APIs)

**3. Use Appropriate Tools**

- **Jaeger/Grafana**: For trace visualization
- **Kibana/Elasticsearch**: For log aggregation and search
- **Prometheus**: For metrics querying and alerting

### Performance Considerations

**1. Sampling Impact**

- Development: 100% sampling for full visibility
- Production: 10% sampling balances observability vs. performance
- High-traffic endpoints: May need lower sampling

**2. Resource Usage**

- OpenTelemetry overhead is typically <2-5% CPU
- Memory usage scales with active traces
- Network usage depends on sampling rate and data volume

---

## Troubleshooting

### Common Issues

**No Logs Appearing**

```bash
# Check if OpenTelemetry is enabled
grep "EnableLogging.*true" appsettings.json

# Verify collector connectivity
curl -f http://localhost:4317/v1/logs

# Check application startup logs for OpenTelemetry errors
```

**Missing Trace Correlation**

```json
{
  "problem": "TraceId missing in some logs",
  "solution": "Ensure Activity.Current is available in logging context"
}
```

**Performance Degradation**

```json
{
  "symptoms": "High CPU or memory usage",
  "causes": [
    "100% sampling in production",
    "Too many custom attributes",
    "Large request/response bodies"
  ]
}
```

### Configuration Issues

**Wrong Collector URL**

```bash
# Test connectivity
curl -f http://your-collector:4317/v1/traces

# Check application logs
grep -i "opentelemetry\|collector" logs/app.log
```

**Sampling Too High/Low**

```json
{
  "development": {
    "TraceSamplingRate": 1.0
  },
  "production": {
    "TraceSamplingRate": 0.1
  }
}
```

### Getting Help

**1. Check Infrastructure Team**

- Collector configuration and connectivity
- Sampling rate adjustments
- Dashboard setup and alerts

**2. Review Documentation**

- [OpenTelemetry .NET Library Documentation](external-link)
- [OpenTelemetry Specification](https://opentelemetry.io/docs/)
- WebShop API observability runbooks

**3. Development Support**

- Use TRACE level for detailed debugging (development only)
- Enable debug logging temporarily: `"OpenTelemetry": "Debug"`
- Test locally with Jaeger: `docker run -p 16686:16686 jaegertracing/all-in-one`

---

## Key Takeaways

- **Automatic**: Most observability works without code changes
- **Correlated**: Use TraceId to connect logs, traces, and metrics
- **Secure**: Sensitive data is automatically protected
- **Performant**: Minimal overhead with appropriate sampling
- **Business-Focused**: Includes WebShop-specific context and attributes

For questions about OpenTelemetry configuration or usage in the WebShop API, contact the infrastructure team or create an issue in the project repository.
