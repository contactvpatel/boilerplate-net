# Rate Limiting Guidelines

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Why Rate Limiting?](#why-rate-limiting)
- [Implementation](#implementation)
- [Usage](#usage)
- [Partition Key Strategy](#partition-key-strategy)
- [Error Responses](#error-responses)
- [Best Practices](#best-practices)
- [Disabling Rate Limiting](#disabling-rate-limiting)
- [Troubleshooting](#troubleshooting)
- [Microsoft Guidelines](#microsoft-guidelines)
- [References](#references)
- [Summary](#summary)

---

## Overview

Rate limiting is a critical security feature that protects your API from abuse, brute force attacks, and DDoS (Distributed Denial of Service) attacks. This project implements ASP.NET Core's built-in rate limiting middleware (available in .NET 7+) following Microsoft guidelines and industry best practices.

## Why Rate Limiting?

Rate limiting provides several important benefits:

- **Prevents API Abuse**: Limits the number of requests a client can make within a time window
- **Protects Against Brute Force Attacks**: Restricts authentication attempts to prevent credential guessing
- **Mitigates DDoS Attacks**: Prevents resource exhaustion from excessive requests
- **Ensures Fair Usage**: Distributes API resources fairly among all clients

## Implementation

### Architecture

The rate limiting implementation uses ASP.NET Core's `PartitionedRateLimiter` which provides:

- **Per-User/IP Rate Limiting**: Each user or IP address has its own rate limit counter
- **Multiple Policies**: Different rate limits for different endpoint types
- **Configurable Algorithms**: Support for FixedWindow, SlidingWindow, TokenBucket, and Concurrency limiters
- **Standardized Error Responses**: Consistent error format with `Retry-After` headers (RFC 6585)

### Configuration

Rate limiting is configured in `appsettings.json` under the `RateLimitingOptions` section:

```json
{
  "RateLimitingOptions": {
    "Enabled": true,
    "GlobalPolicy": {
      "PermitLimit": 100,
      "WindowMinutes": 1,
      "QueueLimit": 0,
      "Algorithm": "FixedWindow"
    },
    "StrictPolicy": {
      "PermitLimit": 10,
      "WindowMinutes": 1,
      "QueueLimit": 0,
      "Algorithm": "FixedWindow"
    },
    "PermissivePolicy": {
      "PermitLimit": 200,
      "WindowMinutes": 1,
      "QueueLimit": 10,
      "Algorithm": "FixedWindow"
    }
  }
}
```

### Configuration Options

#### `Enabled`

- **Type**: `bool`
- **Default**: `true`
- **Description**: Enables or disables rate limiting globally. When disabled, all rate limiting is bypassed.

#### `GlobalPolicy`

- **Type**: `RateLimitPolicy`
- **Description**: Default rate limiting policy applied to all endpoints unless overridden.
- **Default**: 100 requests per minute

#### `StrictPolicy`

- **Type**: `RateLimitPolicy`
- **Description**: Strict rate limiting for sensitive endpoints (authentication, write operations).
- **Default**: 10 requests per minute
- **Use Case**: Apply to authentication endpoints, password reset, payment processing, etc.

#### `PermissivePolicy`

- **Type**: `RateLimitPolicy`
- **Description**: Permissive rate limiting for read-only endpoints.
- **Default**: 200 requests per minute with queue limit of 10
- **Use Case**: Apply to GET endpoints, data retrieval operations, etc.

### Policy Configuration (`RateLimitPolicy`)

#### `PermitLimit`

- **Type**: `int`
- **Default**: `100`
- **Description**: Maximum number of requests allowed in the time window.

#### `WindowMinutes`

- **Type**: `int`
- **Default**: `1`
- **Description**: Time window in minutes for the rate limit.

#### `QueueLimit`

- **Type**: `int`
- **Default**: `0`
- **Description**: Maximum number of queued requests when limit is reached.
  - `0`: Reject requests immediately (default)
  - `> 0`: Queue requests up to the limit, then reject

#### `Algorithm`

- **Type**: `string`
- **Default**: `"FixedWindow"`
- **Options**: `"FixedWindow"`, `"SlidingWindow"`, `"TokenBucket"`, `"Concurrency"`
- **Description**: Rate limiting algorithm to use.

**Algorithm Comparison:**

| Algorithm | Description | Best For |
|-----------|-------------|----------|
| **FixedWindow** | Simple, predictable limits within fixed time windows | General purpose, simple use cases |
| **SlidingWindow** | More accurate, prevents bursts at window boundaries | When you need smoother rate limiting |
| **TokenBucket** | Allows bursts up to bucket size, then steady rate | APIs that need to handle traffic spikes |
| **Concurrency** | Limits concurrent requests, not request rate | Long-running operations, resource-intensive endpoints |

### TokenBucket-Specific Options

When using `"TokenBucket"` algorithm:

#### `TokensPerPeriod`

- **Type**: `int`
- **Default**: `10`
- **Description**: Number of tokens added per replenishment period.

#### `ReplenishmentPeriodSeconds`

- **Type**: `int`
- **Default**: `10`
- **Description**: Time period in seconds for token replenishment.

#### `TokenLimit`

- **Type**: `int`
- **Default**: `20`
- **Description**: Maximum number of tokens in the bucket (allows bursts).

### Concurrency-Specific Options

When using `"Concurrency"` algorithm:

#### `PermitLimitConcurrency`

- **Type**: `int`
- **Default**: `10`
- **Description**: Maximum number of concurrent requests.

#### `QueueLimitConcurrency`

- **Type**: `int`
- **Default**: `0`
- **Description**: Maximum number of queued requests when concurrency limit is reached.

## Usage

### Global Rate Limiter

The global rate limiter applies to **all endpoints** by default. No additional code is required - it's automatically enforced.

**Default Behavior:**

- 100 requests per minute per user/IP
- Uses partition key (user ID or IP address)
- Rejects requests immediately when limit is exceeded

### Applying Policies to Endpoints

Use the `[EnableRateLimiting]` attribute to apply specific policies to endpoints:

#### Strict Policy (Sensitive Endpoints)

```csharp
[HttpPost("renew-token")]
[AllowAnonymous]
[EnableRateLimiting("strict")]  // 10 requests/minute
[ProducesResponseType(typeof(Response<SsoAuthResponse>), StatusCodes.Status200OK)]
public async Task<ActionResult<Response<SsoAuthResponse>>> RenewToken(
    [FromBody] SsoRenewTokenRequest request,
    CancellationToken cancellationToken)
{
    // Implementation
}
```

**Use Cases:**

- Authentication endpoints (`/login`, `/renew-token`)
- Password reset endpoints
- Payment processing
- Write operations (POST, PUT, DELETE, PATCH)
- Any sensitive operations

#### Permissive Policy (Read-Only Endpoints)

```csharp
[HttpGet("products")]
[EnableRateLimiting("permissive")]  // 200 requests/minute with queue
[ProducesResponseType(typeof(Response<List<ProductDto>>), StatusCodes.Status200OK)]
public async Task<ActionResult<Response<List<ProductDto>>>> GetProducts(
    CancellationToken cancellationToken)
{
    // Implementation
}
```

**Use Cases:**

- GET endpoints
- Data retrieval operations
- Public read-only APIs
- Search endpoints

### Controller-Level Application

You can apply rate limiting at the controller level:

```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/sensitive")]
[EnableRateLimiting("strict")]  // Applies to all endpoints in this controller
public class SensitiveController : BaseApiController
{
    // All endpoints inherit the "strict" policy
}
```

### Disabling Rate Limiting for Specific Endpoints

To disable rate limiting for a specific endpoint (not recommended for production):

```csharp
[HttpGet("health")]
[DisableRateLimiting]  // Bypasses rate limiting
public IActionResult Health()
{
    return Ok();
}
```

## Partition Key Strategy

The rate limiting implementation uses a partition key to identify unique clients. The partition key is determined in the following priority order:

1. **Authenticated User ID**: If the user is authenticated, uses `user:{userId}`
2. **User ID from HttpContext.Items**: If set by JWT filter, uses `user:{userId}`
3. **IP Address**: For anonymous users, uses `ip:{ipAddress}`
4. **Anonymous**: Fallback to `"anonymous"` if no identifier is available

This ensures:

- **Per-User Rate Limiting**: Authenticated users have individual rate limits
- **Per-IP Rate Limiting**: Anonymous users are rate-limited by IP address
- **Fair Distribution**: Each client gets their own rate limit counter

## Error Responses

When a rate limit is exceeded, the API returns a standardized error response:

**HTTP Status Code**: `429 Too Many Requests`

**Response Body:**

```json
{
  "succeeded": false,
  "data": null,
  "message": "Rate limit exceeded. Please try again later.",
  "errors": [
    {
      "errorId": "guid-here",
      "statusCode": 429,
      "message": "Too many requests. Please retry after the specified time."
    }
  ]
}
```

**Response Headers:**

- `Retry-After`: Number of seconds to wait before retrying (RFC 6585)

## Best Practices

### 1. **Choose Appropriate Limits**

- **Authentication Endpoints**: 5-10 requests/minute (strict)
- **Write Operations**: 10-50 requests/minute (strict)
- **Read Operations**: 100-200 requests/minute (permissive)
- **Public APIs**: Consider higher limits with monitoring

### 2. **Use Queue Limits for Read Operations**

For read-only endpoints, consider using a queue limit to handle traffic spikes:

```json
{
  "PermissivePolicy": {
    "PermitLimit": 200,
    "WindowMinutes": 1,
    "QueueLimit": 10,  // Queue up to 10 requests
    "Algorithm": "FixedWindow"
  }
}
```

### 3. **Monitor Rate Limit Violations**

Monitor `429 Too Many Requests` responses to:

- Identify potential attacks
- Adjust rate limits based on legitimate traffic patterns
- Detect API abuse early

### 4. **Environment-Specific Configuration**

Use different rate limits for different environments:

**Development:**

```json
{
  "GlobalPolicy": {
    "PermitLimit": 1000,  // Higher limit for development
    "WindowMinutes": 1
  }
}
```

**Production:**

```json
{
  "GlobalPolicy": {
    "PermitLimit": 100,  // Stricter limit for production
    "WindowMinutes": 1
  }
}
```

### 5. **Combine with Authentication**

Rate limiting works best when combined with authentication:

- Authenticated users get per-user rate limits
- Anonymous users get per-IP rate limits
- Prevents IP-based attacks from affecting authenticated users

### 6. **Consider Distributed Rate Limiting**

For multi-instance deployments, consider using a distributed cache (Redis) for rate limiting to ensure consistent limits across all instances. The current implementation uses in-memory rate limiting, which is per-instance.

## Disabling Rate Limiting

To disable rate limiting globally:

```json
{
  "RateLimitingOptions": {
    "Enabled": false
  }
}
```

**Note**: Disabling rate limiting is **not recommended** for production environments as it removes an important security layer.

## Troubleshooting

### Issue: Rate Limits Too Strict

**Symptoms**: Legitimate users receiving `429 Too Many Requests` errors

**Solutions**:

1. Increase `PermitLimit` in the appropriate policy
2. Increase `WindowMinutes` to allow more requests over a longer period
3. Use `QueueLimit` to queue requests instead of rejecting immediately
4. Consider using `SlidingWindow` algorithm for smoother rate limiting

### Issue: Rate Limits Too Permissive

**Symptoms**: API abuse, high resource usage, potential DDoS

**Solutions**:

1. Decrease `PermitLimit` in the appropriate policy
2. Apply `strict` policy to more endpoints
3. Monitor rate limit violations and adjust accordingly

### Issue: Rate Limits Not Applied

**Possible Causes**:

1. `Enabled` is set to `false` in configuration
2. `UseRateLimiter()` middleware is not in the pipeline
3. Endpoint has `[DisableRateLimiting]` attribute

**Solutions**:

1. Verify `RateLimitingOptions.Enabled` is `true`
2. Check `Middleware/MiddlewareExtensions.cs` for `app.UseRateLimiter()`
3. Remove `[DisableRateLimiting]` attribute if present

## Microsoft Guidelines

This implementation follows Microsoft's recommended practices:

- ✅ Uses built-in ASP.NET Core Rate Limiting middleware
- ✅ Implements per-user/IP partitioning for accurate rate limiting
- ✅ Provides standardized error responses with `Retry-After` headers
- ✅ Supports multiple rate limiting algorithms
- ✅ Configurable via `appsettings.json`
- ✅ Early in middleware pipeline (after HTTPS enforcement)

## References

- [Microsoft: Rate Limiting in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit)
- [RFC 6585: Additional HTTP Status Codes](https://www.rfc-editor.org/rfc/rfc6585.html)
- [OWASP: API Security Top 10](https://owasp.org/www-project-api-security/)
- [ASP.NET Core Rate Limiting Source Code](https://github.com/dotnet/aspnetcore/tree/main/src/Middleware/RateLimiting)

---

## Summary

Rate limiting is a critical security feature that protects your API from abuse and ensures fair resource distribution. This implementation provides:

- ✅ **Global rate limiting** (100 requests/minute default)
- ✅ **Strict policy** for sensitive endpoints (10 requests/minute)
- ✅ **Permissive policy** for read-only endpoints (200 requests/minute)
- ✅ **Per-user/IP partitioning** for accurate rate limiting
- ✅ **Configurable via `appsettings.json`**
- ✅ **Standardized error responses** with `Retry-After` headers
- ✅ **Multiple algorithms** (FixedWindow, SlidingWindow, TokenBucket, Concurrency)

Apply rate limiting policies to endpoints using the `[EnableRateLimiting]` attribute, and monitor rate limit violations to adjust limits based on your API's traffic patterns.
