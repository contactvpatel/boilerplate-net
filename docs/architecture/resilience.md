# Resilience Patterns Guide

## Overview

Resilience is the ability of an application to handle failures gracefully and recover from transient errors. This project implements resilience patterns using `Microsoft.Extensions.Http.Resilience` (the modern replacement for the deprecated `Microsoft.Extensions.Http.Polly` package) following Microsoft .NET 10 best practices and guidelines.

**Note:** While this guide focuses on HTTP client resilience, the same patterns and principles can be applied to other scenarios (database operations, external service calls, etc.).

[← Back to README](../../README.md)

## Table of Contents

- [Why Resilience?](#why-resilience)
- [Resilience Strategies](#resilience-strategies)
- [Standard Resilience Handler](#standard-resilience-handler)
- [Configuration](#configuration)
- [Implementation Details](#implementation-details)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Why Resilience?

Modern applications depend on external services, networks, and resources that can fail. Resilience patterns help applications:

- **Recover from transient failures** automatically
- **Prevent cascading failures** when dependencies are down
- **Protect downstream services** from overload
- **Improve user experience** by handling errors gracefully
- **Reduce operational costs** by avoiding unnecessary retries

### Common Failure Scenarios

- **Network timeouts** - Slow or unresponsive networks
- **Service unavailability** - External services temporarily down
- **Rate limiting** - Services throttling requests
- **Transient errors** - Temporary issues that resolve quickly
- **Resource exhaustion** - Overloaded services or databases

## Resilience Strategies

### 1. Retry Pattern

**Purpose:** Automatically retry failed operations that may succeed on subsequent attempts.

**When to Use:**
- Transient network errors
- Temporary service unavailability
- Rate limiting (429 Too Many Requests)
- Timeout errors

**Configuration:**
- **Max Retry Attempts**: 3 attempts (configurable)
- **Backoff Strategy**: Exponential with jitter
- **Base Delay**: 2 seconds (results in 2s, 4s, 8s delays)
- **Retries On**: Server errors (5xx), Request Timeout (408), Too Many Requests (429), and network exceptions

**Implementation:**
```csharp
.AddStandardResilienceHandler(options =>
{
    options.Retry.MaxRetryAttempts = resilienceOptions.MaxRetryAttempts;
    options.Retry.Delay = TimeSpan.FromSeconds(resilienceOptions.RetryBaseDelaySeconds);
    // Exponential backoff and jitter are enabled by default
});
```

**Benefits:**
- Automatic recovery from transient failures
- Exponential backoff prevents overwhelming failing services
- Jitter randomizes retry timing to prevent thundering herd
- Configurable per environment

**Best Practices:**
- Use exponential backoff to avoid overwhelming services
- Add jitter to prevent synchronized retries
- Limit retry attempts to avoid long delays
- Only retry on transient errors (not 4xx client errors)

### 2. Circuit Breaker Pattern

**Purpose:** Prevent cascading failures by "opening" the circuit when a service is failing, allowing it to recover.

**When to Use:**
- External service dependencies
- Database connections
- Any operation that can fail repeatedly

**Configuration:**
- **Failure Ratio**: 10% (circuit opens when 10% of requests fail)
- **Minimum Throughput**: 5 requests (minimum requests needed before circuit can open)
- **Sampling Duration**: 30 seconds (time window for failure evaluation)
- **Break Duration**: 30 seconds (how long circuit stays open)

**Implementation:**
```csharp
.AddStandardResilienceHandler(options =>
{
    options.CircuitBreaker.FailureRatio = 0.1; // 10% failure ratio
    options.CircuitBreaker.MinimumThroughput = resilienceOptions.CircuitBreakerMinimumThroughput;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(resilienceOptions.CircuitBreakerBreakDurationSeconds);
});
```

**Circuit States:**
1. **Closed** (Normal) - Requests flow through normally
2. **Open** (Failing) - Requests fail fast, no calls to failing service
3. **Half-Open** (Testing) - Allows one request to test if service recovered

**Benefits:**
- Prevents cascading failures when external services are down
- Fast failure instead of waiting for timeouts
- Automatic recovery after break duration expires
- Protects downstream services from overload

**Best Practices:**
- Set appropriate failure ratio (10-20% is typical)
- Use minimum throughput to avoid opening on low traffic
- Monitor circuit breaker state changes
- Log circuit state transitions for observability

### 3. Timeout Pattern

**Purpose:** Prevent operations from hanging indefinitely by enforcing time limits.

**When to Use:**
- All external service calls
- Database queries
- Long-running operations

**Configuration:**
- **Total Timeout**: Overall timeout for entire operation including retries (configurable per service)
- **Attempt Timeout**: Timeout for each individual attempt (10 seconds default)

**Implementation:**
```csharp
.AddStandardResilienceHandler(options =>
{
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    // Attempt timeout is 10 seconds by default
});
```

**Benefits:**
- Prevents indefinite waiting
- Frees resources quickly
- Improves user experience with faster failures

**Best Practices:**
- Set timeouts based on service SLAs
- Use shorter timeouts for critical paths
- Consider network latency in timeout calculations
- Use different timeouts for different operations

### 4. Rate Limiter Pattern

**Purpose:** Limit the number of concurrent requests to prevent overwhelming services.

**When to Use:**
- External API calls with rate limits
- Database connections
- Resource-intensive operations

**Configuration:**
- **Permit Limit**: 1000 concurrent requests (default)
- **Queue Limit**: 0 (reject immediately when limit reached)

**Implementation:**
```csharp
.AddStandardResilienceHandler(options =>
{
    // Rate limiter is configured by default (1000 permits, queue: 0)
    // Can be customized if needed
});
```

**Benefits:**
- Prevents overwhelming downstream services
- Protects against resource exhaustion
- Ensures fair resource distribution

## Standard Resilience Handler

The `AddStandardResilienceHandler()` provides a production-ready resilience pipeline with five strategies automatically chained in the correct order:

### Pipeline Order (Outermost to Innermost)

1. **Rate Limiter** - Limits concurrent requests (1000 permits, queue: 0)
2. **Total Timeout** - Overall timeout for entire request including retries
3. **Retry** - Retries on transient errors with exponential backoff
4. **Circuit Breaker** - Opens circuit after too many failures
5. **Attempt Timeout** - Timeout for each individual attempt (10s)

### Why This Order Matters

The order ensures:
- **Rate limiting** happens first to prevent overload
- **Total timeout** encompasses all retries
- **Retry** attempts happen before circuit breaker evaluation
- **Circuit breaker** prevents unnecessary retries when service is down
- **Attempt timeout** limits each individual request

### Implementation Example

```csharp
services.AddHttpClient("MyService", client =>
{
    client.BaseAddress = new Uri("https://api.example.com");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddStandardResilienceHandler(options =>
{
    // Configure total timeout
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);

    // Configure retry strategy
    options.Retry.MaxRetryAttempts = 3;
    options.Retry.Delay = TimeSpan.FromSeconds(2);

    // Configure circuit breaker strategy
    options.CircuitBreaker.FailureRatio = 0.1; // 10% failure ratio
    options.CircuitBreaker.MinimumThroughput = 5;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
});
```

### Execution Flow

1. Request sent
2. Rate limiter checks concurrent request limit
3. Total timeout starts (includes all retries)
4. If transient error → Retry (up to configured attempts with exponential backoff)
5. If still failing → Circuit breaker counts failure
6. After threshold failures → Circuit opens, requests fail fast
7. After break duration → Circuit half-opens, tests service
8. If successful → Circuit closes, normal operation resumes
9. Attempt timeout limits each individual request attempt

## Configuration

Resilience options are configured in `appsettings.json` under the `HttpResilienceOptions` section:

```json
{
  "HttpResilienceOptions": {
    "MaxRetryAttempts": 3,
    "RetryBaseDelaySeconds": 2,
    "CircuitBreakerMinimumThroughput": 5,
    "CircuitBreakerBreakDurationSeconds": 30
  }
}
```

### Configuration Options

| Property | Default | Description |
|----------|---------|-------------|
| `MaxRetryAttempts` | 3 | Maximum number of retry attempts for transient failures |
| `RetryBaseDelaySeconds` | 2 | Base delay in seconds for exponential backoff (results in 2s, 4s, 8s delays) |
| `CircuitBreakerMinimumThroughput` | 5 | Minimum number of requests required before circuit breaker can evaluate failures |
| `CircuitBreakerBreakDurationSeconds` | 30 | Duration in seconds that circuit breaker stays open before attempting to close |

**Note:** These properties use `[JsonPropertyName]` attributes for backward compatibility. You can use either the new property names (`MaxRetryAttempts`) or the old names (`RetryCount`) in `appsettings.json`.

### Environment-Specific Configuration

```json
// appsettings.Development.json
{
  "HttpResilienceOptions": {
    "MaxRetryAttempts": 2,
    "CircuitBreakerMinimumThroughput": 3
  }
}

// appsettings.Production.json
{
  "HttpResilienceOptions": {
    "MaxRetryAttempts": 5,
    "CircuitBreakerMinimumThroughput": 10,
    "CircuitBreakerBreakDurationSeconds": 60
  }
}
```

## Implementation Details

### HTTP Client Resilience

For HTTP client resilience, see the [HttpClient Factory Guide](httpclient-factory.md) for detailed implementation examples.

### Custom Resilience Pipelines

For scenarios beyond HTTP clients, you can create custom resilience pipelines:

```csharp
services.AddResiliencePipeline("CustomPipeline", builder =>
{
    builder.AddRetry(new RetryStrategyOptions
    {
        MaxRetryAttempts = 3,
        BackoffType = DelayBackoffType.Exponential,
        UseJitter = true,
        BaseDelay = TimeSpan.FromSeconds(2)
    });

    builder.AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.1,
        MinimumThroughput = 5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        BreakDuration = TimeSpan.FromSeconds(30)
    });

    builder.AddTimeout(TimeSpan.FromSeconds(10));
});
```

## Best Practices

### 1. **Choose Appropriate Retry Counts**

- **Low-latency services**: 1-2 retries
- **Standard services**: 3 retries (default)
- **High-latency services**: 3-5 retries
- **Critical operations**: 5+ retries with longer delays

### 2. **Use Exponential Backoff**

Always use exponential backoff to prevent overwhelming failing services:
- Base delay: 2 seconds
- Retry 1: 2s delay
- Retry 2: 4s delay
- Retry 3: 8s delay

### 3. **Add Jitter**

Enable jitter to randomize retry timing and prevent thundering herd:
- Prevents synchronized retries from multiple clients
- Reduces load spikes on recovering services

### 4. **Configure Circuit Breakers Appropriately**

- **Failure Ratio**: 10-20% is typical
- **Minimum Throughput**: Set based on expected traffic (5-10 for low traffic, 100+ for high traffic)
- **Break Duration**: 30-60 seconds is typical
- **Sampling Duration**: Should be long enough to capture failure patterns (30 seconds default)

### 5. **Set Realistic Timeouts**

- **Total Timeout**: Should accommodate all retries (e.g., 30s for 3 retries with 2s base delay)
- **Attempt Timeout**: Should be shorter than total timeout (e.g., 10s per attempt)
- **Service-Specific**: Different services may need different timeouts

### 6. **Monitor Resilience Metrics**

Monitor:
- Retry attempt counts
- Circuit breaker state changes
- Timeout occurrences
- Failure rates

### 7. **Log Resilience Events**

Log important resilience events:
- Retry attempts (Warning level)
- Circuit breaker state changes (Warning/Information level)
- Timeout occurrences (Warning level)

## Troubleshooting

### Issue: Too Many Retries

**Symptoms:** Requests taking too long, high latency

**Solutions:**
1. Reduce `MaxRetryAttempts`
2. Reduce `RetryBaseDelaySeconds`
3. Check if errors are truly transient
4. Review timeout settings

### Issue: Circuit Breaker Opening Too Frequently

**Symptoms:** Circuit breaker opens even when service is healthy

**Solutions:**
1. Increase `CircuitBreakerMinimumThroughput`
2. Increase `CircuitBreakerBreakDurationSeconds`
3. Review failure ratio (may be too low)
4. Check if errors are transient vs. persistent

### Issue: Circuit Breaker Not Opening When It Should

**Symptoms:** Service is failing but circuit breaker stays closed

**Solutions:**
1. Decrease `CircuitBreakerMinimumThroughput`
2. Decrease failure ratio
3. Check if errors are being handled correctly
4. Review sampling duration

### Issue: Timeouts Too Aggressive

**Symptoms:** Legitimate requests timing out

**Solutions:**
1. Increase `TotalRequestTimeout`
2. Increase `AttemptTimeout`
3. Review service SLAs
4. Consider service-specific timeouts

### Issue: High Resource Usage

**Symptoms:** High CPU/memory usage, connection exhaustion

**Solutions:**
1. Reduce `MaxRetryAttempts`
2. Reduce rate limiter permit limit
3. Review circuit breaker settings
4. Check for connection leaks

## Microsoft Guidelines

This implementation follows Microsoft's recommended practices:

- ✅ Uses `Microsoft.Extensions.Http.Resilience` (modern replacement for deprecated `Microsoft.Extensions.Http.Polly`)
- ✅ Implements standard resilience handler with five built-in strategies
- ✅ Configurable via `appsettings.json`
- ✅ Follows .NET 10 best practices
- ✅ Supports environment-specific configuration
- ✅ Provides structured logging for resilience events

## References

- [Microsoft: Build resilient HTTP apps](https://learn.microsoft.com/en-us/dotnet/core/resilience/http-resilience)
- [Microsoft: Introduction to resilient app development](https://learn.microsoft.com/en-us/dotnet/core/resilience/)
- [Polly: Resilience and transient-fault-handling library](https://www.pollydocs.org/)
- [HttpClient Factory Guide](httpclient-factory.md) - HTTP client-specific resilience implementation

---

## Summary

Resilience patterns are essential for building robust, production-ready applications. This project implements:

- ✅ **Standard Resilience Handler** with five built-in strategies
- ✅ **Retry Pattern** with exponential backoff and jitter
- ✅ **Circuit Breaker Pattern** to prevent cascading failures
- ✅ **Timeout Pattern** to prevent indefinite waiting
- ✅ **Rate Limiter Pattern** to prevent overload
- ✅ **Configurable via `appsettings.json`**
- ✅ **Environment-specific configuration support**

For HTTP client-specific resilience implementation, see the [HttpClient Factory Guide](httpclient-factory.md).

