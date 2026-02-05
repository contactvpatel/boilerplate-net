# JWT Token Authentication Filter Implementation Guide

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Why JwtTokenAuthenticationFilter Exists](#why-jwttokenauthenticationfilter-exists)
- [What Problem It Solves](#what-problem-it-solves)
- [How It Works](#how-it-works)
- [Architecture & Design](#architecture--design)
- [Security Considerations](#security-considerations)
- [Benefits](#benefits)
- [Implementation Details](#implementation-details)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

---

## Overview

The `JwtTokenAuthenticationFilter` is a global authorization filter that validates JWT (JSON Web Token) tokens from a custom SSO (Single Sign-On) service. This filter ensures that only authenticated users can access protected API endpoints, providing a centralized authentication mechanism that follows security best practices.

## Why JwtTokenAuthenticationFilter Exists

### The Problem with Manual Authentication

Without a centralized authentication filter, each controller or action would need to manually:
1. Extract the token from the `Authorization` header
2. Parse and validate the JWT token
3. Check token expiration
4. Verify token with SSO service
5. Extract user information
6. Handle authentication failures

**Issues with this approach:**
- **Code Duplication**: Authentication logic repeated across controllers
- **Security Risk**: Easy to forget authentication checks
- **Inconsistency**: Different error handling across endpoints
- **Maintenance Burden**: Changes require updates in multiple places
- **Violates DRY Principle**: Don't Repeat Yourself

### The Solution

`JwtTokenAuthenticationFilter` centralizes authentication logic and automatically:
1. Validates JWT tokens for all protected endpoints
2. Returns standardized error responses for authentication failures
3. Extracts and stores user information in HTTP context
4. Supports `[AllowAnonymous]` attribute for public endpoints
5. Logs authentication attempts for security monitoring

## What Problem It Solves

### 1. **Security**
Ensures all protected endpoints require valid authentication, preventing unauthorized access.

### 2. **Consistency**
All authentication failures return the same standardized error format, providing a consistent API experience.

### 3. **Separation of Concerns**
Controllers focus on business logic, not authentication. Authentication is handled at the filter level.

### 4. **Maintainability**
Authentication logic is centralized. Changes to token validation or error handling only need to be made in one place.

### 5. **Observability**
All authentication attempts (successful and failed) are logged with context, enabling security monitoring and debugging.

### 6. **Developer Experience**
Developers don't need to remember to add authentication checks. It happens automatically for all endpoints (except those marked with `[AllowAnonymous]`).

## How It Works

### Execution Flow

```
1. Client sends HTTP request with Authorization header
   ↓
2. JwtTokenAuthenticationFilter.OnAuthorizationAsync() is called
   ↓
3. Check if endpoint has [AllowAnonymous] attribute
   ↓
3a. If AllowAnonymous:
    - Skip authentication
    - Continue to next filter/action
   ↓
3b. If not AllowAnonymous:
    - Extract Bearer token from Authorization header
   ↓
4. Validate token presence
   ↓
4a. If missing/invalid:
    - Log warning
    - Return 401 Unauthorized with standardized response
    - Stop execution
   ↓
4b. If present:
    - Parse JWT token
   ↓
5. Validate token structure
   ↓
5a. If invalid format:
    - Log warning
    - Return 401 Unauthorized
    - Stop execution
   ↓
5b. If valid format:
    - Check token expiration
   ↓
6. Validate token expiration
   ↓
6a. If expired:
    - Log warning
    - Return 401 Unauthorized
    - Stop execution
   ↓
6b. If not expired:
    - Generate cache key from token hash
    - Check cache for validation result
   ↓
7. Cache-First Token Validation
   ↓
7a. If cache hit:
    - Return cached validation result
   ↓
7b. If cache miss:
    - Validate token with SSO service
    - Cache result with expiration = token expiration
   ↓
7c. If cache error:
    - Fall back to direct SSO validation
   ↓
8. SSO Service Validation Result
   ↓
8a. If invalid:
    - Log warning
    - Return 401 Unauthorized
    - Stop execution
   ↓
8b. If valid:
    - Extract user ID from token
    - Store user ID and token in HTTP context
    - Log success (debug level)
    - Continue to next filter/action
```

### Token Extraction

The filter extracts the Bearer token from the `Authorization` header:

```http
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Format**: `Bearer {token}`

### Token Validation Steps

1. **Presence Check**: Token must be present in `Authorization` header
2. **Format Check**: Token must be a valid JWT format
3. **Expiration Check**: Token must not be expired (checks `exp` claim)
4. **Cache Check**: Check if token validation result is cached
5. **SSO Validation**: If not cached, validate token with SSO service
6. **Cache Result**: Cache validation result until token expiration

### User Context Storage

After successful authentication, user information is stored in `HttpContext.Items`:

```csharp
_httpContextAccessor.HttpContext?.Items.TryAdd("UserId", userId);
_httpContextAccessor.HttpContext?.Items.TryAdd("UserToken", token);
```

**Usage in Controllers/Services:**
```csharp
string? userId = _httpContextAccessor.HttpContext?.Items["UserId"]?.ToString();
```

## Architecture & Design

### Filter Registration

The filter is registered globally in `Core/ServiceExtensions.cs`:

```csharp
services.AddControllers(options =>
{
    options.Filters.Add<JwtTokenAuthenticationFilter>(); // Global filter
    options.Filters.Add<ValidationFilter>();
});
```

**Why Global?**
- Ensures all endpoints are protected by default
- No need to remember to add authentication to each controller
- Security by default (opt-out with `[AllowAnonymous]`)

### Filter Order

Filters execute in this order:
1. `JwtTokenAuthenticationFilter` (authentication)
2. `ValidationFilter` (validation)
3. Controller action

**Why this order?**
- Authentication happens first (security)
- Validation happens after authentication (efficiency - don't validate if not authenticated)
- Business logic executes last

### AllowAnonymous Support

Endpoints can opt out of authentication using `[AllowAnonymous]`:

```csharp
[AllowAnonymous]
[HttpGet("public")]
public IActionResult PublicEndpoint()
{
    return Ok("This endpoint is public");
}
```

**Use Cases:**
- Health check endpoints
- Public API documentation
- Authentication/login endpoints
- Public data endpoints

## Security Considerations

### 1. **Token Validation**
- Tokens are validated with the SSO service, not just parsed
- Prevents use of revoked or invalid tokens
- Ensures tokens are issued by the trusted SSO service
- Validation results are cached to reduce SSO service load

### 2. **Expiration Checking**
- Tokens are checked for expiration before validation
- Expired tokens are rejected immediately
- Reduces unnecessary SSO service calls
- Cache expiration matches token expiration

### 3. **Cache Security**
- **Token hash used for cache key** (not the full token)
- SHA256 hash prevents token exposure in cache keys
- Cache key format: `jwt_token:{sha256_hash}` (74 characters)
- Cache expires when token expires (automatic cleanup)

### 4. **Error Information**
- Authentication failures return generic messages
- Detailed error information is logged, not exposed to clients
- Prevents information leakage to attackers

### 5. **Logging**
- All authentication attempts are logged
- Failed attempts include context (path, method, user ID if available)
- Cache hits/misses logged at debug level
- Enables security monitoring and incident response

### 6. **HTTPS Enforcement**
- The API enforces HTTPS-only access
- Tokens are transmitted over encrypted connections
- Prevents token interception

### 7. **Fail-Open Strategy**
- If cache fails, falls back to direct SSO validation
- Ensures availability even if cache service is unavailable
- Prevents cache failures from blocking authentication

## Benefits

### 1. **Security by Default**
All endpoints are protected unless explicitly marked with `[AllowAnonymous]`.

### 2. **Centralized Security Logic**
Authentication logic is in one place, making security updates easier.

### 3. **Consistent Error Responses**
All authentication failures return the same format:
```json
{
  "succeeded": false,
  "data": null,
  "message": "Authentication failed",
  "errors": [
    {
      "errorId": "guid",
      "statusCode": 401,
      "message": "Token is missing or invalid"
    }
  ]
}
```

### 4. **Better Observability**
Authentication events are logged with structured logging:
- Successful authentications (debug level)
- Failed authentications (warning level)
- Context information (path, method, user ID)

### 5. **Performance**
- Token expiration is checked before SSO service call
- Invalid tokens are rejected early
- **Token validation results are cached** until token expiration
- Reduces SSO service calls by caching validated tokens
- Cache-first strategy improves response time for repeated requests
- Fallback to direct SSO validation if cache fails (fail-open for availability)

## Implementation Details

### Filter Implementation

```csharp
public class JwtTokenAuthenticationFilter(
    ISsoService ssoService,
    IHttpContextAccessor httpContextAccessor,
    ICacheService cacheService,
    ILogger<JwtTokenAuthenticationFilter> logger) : IAsyncAuthorizationFilter
{
    private const string CacheKeyPrefix = "jwt_token:";
    private const double MinimumCacheTimeSeconds = 1.0;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        // Check AllowAnonymous
        // Extract token
        // Validate token with caching
        // Store user context
    }
}
```

**Key Dependencies:**
- `ISsoService`: Validates tokens with SSO service
- `ICacheService`: Caches validation results (HybridCache)
- `IHttpContextAccessor`: Accesses HTTP context for user storage
- `ILogger`: Logs authentication events

### Token Extraction

Uses `JwtTokenHelper.ExtractBearerToken()`:

```csharp
string? authorizationHeader = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
string? token = JwtTokenHelper.ExtractBearerToken(authorizationHeader);
```

### Token Parsing

Uses `JwtTokenHelper.ParseToken()`:

```csharp
JwtSecurityToken? jwtToken = JwtTokenHelper.ParseToken(token);
```

### Expiration Check

Uses `JwtTokenHelper.IsTokenExpired()`:

```csharp
if (JwtTokenHelper.IsTokenExpired(jwtToken))
{
    // Reject token
}
```

### SSO Validation with Caching

Uses cache-first strategy with `ICacheService.GetOrCreateAsync()`:

```csharp
// Generate cache key from token hash (SHA256)
string cacheKey = GenerateCacheKey(token);

// Calculate cache expiration based on token expiration
TimeSpan? cacheExpiration = CalculateCacheExpiration(jwtToken);

// Cache-first validation
bool isValid = await _cacheService.GetOrCreateAsync(
    cacheKey,
    async cancellationToken =>
    {
        // Only called on cache miss
        return await _ssoService.ValidateTokenAsync(token, cancellationToken);
    },
    expiration: cacheExpiration,
    cancellationToken: context.HttpContext.RequestAborted);
```

**Cache Key Generation:**
- Uses SHA256 hash of the token (64 hex characters)
- Prefix: `"jwt_token:"` (10 characters)
- Total length: **74 characters** (fixed, regardless of token length)
- Example: `jwt_token:a1b2c3d4e5f6789012345678901234567890abcdef1234567890abcdef123456`

**Cache Expiration:**
- Set to token expiration time (from JWT `exp` claim)
- Minimum cache time: 1 second (tokens expiring soon are not cached)
- Cache automatically expires when token expires

**Error Handling:**
- If cache fails, falls back to direct SSO validation (fail-open)
- Ensures availability even if cache service is unavailable

### Error Response

Uses standardized `Response<T>` model:

```csharp
Response<object?> response = new(null, false, "Authentication failed", errors);
context.Result = new UnauthorizedObjectResult(response);
```

## Configuration

### SSO Service Configuration

In `appsettings.json`:

```json
{
  "AppSettings": {
    "EnableAsmAuthorization": true
  }
}
```

**Note:** `EnableAsmAuthorization` controls whether authentication is enabled. When `false`, the filter may be disabled or bypassed.

### SSO Service Implementation

The SSO service is implemented in:
- **Interface**: `src/WebShop.Core/Interfaces/ISsoService.cs`
- **Business Implementation**: `src/WebShop.Business/Services/SsoService.cs`
- **Core Implementation**: `src/WebShop.Infrastructure/Services/SsoService.cs`

### HTTP Context Accessor

Registered in `Core/ServiceExtensions.cs`:

```csharp
services.ConfigureHttpContextAccessor();
```

This enables access to `HttpContext` in services and filters.

## Usage Examples

### Example 1: Authenticated Request

**Request:**
```http
GET /api/v1/customers/123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response:** `200 OK` (authentication passes, customer data returned)

### Example 2: Missing Token

**Request:**
```http
GET /api/v1/customers/123
```

**Response:** `401 Unauthorized`
```json
{
  "succeeded": false,
  "data": null,
  "message": "Authentication failed",
  "errors": [
    {
      "errorId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "statusCode": 401,
      "message": "Token is missing or invalid"
    }
  ]
}
```

### Example 3: Expired Token

**Request:**
```http
GET /api/v1/customers/123
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9... (expired)
```

**Response:** `401 Unauthorized`
```json
{
  "succeeded": false,
  "data": null,
  "message": "Authentication failed",
  "errors": [
    {
      "errorId": "b2c3d4e5-f6a7-8901-bcde-f12345678901",
      "statusCode": 401,
      "message": "Token has expired"
    }
  ]
}
```

### Example 4: Public Endpoint

**Controller:**
```csharp
[AllowAnonymous]
[HttpGet("health")]
public IActionResult Health()
{
    return Ok(new { status = "healthy" });
}
```

**Request:**
```http
GET /api/v1/health
```

**Response:** `200 OK` (no authentication required)

### Example 5: Accessing User Context

**Controller:**
```csharp
[HttpGet("profile")]
public async Task<ActionResult<Response<CustomerDto>>> GetProfile(
    IHttpContextAccessor httpContextAccessor,
    CancellationToken cancellationToken)
{
    string? userId = httpContextAccessor.HttpContext?.Items["UserId"]?.ToString();
    
    if (string.IsNullOrWhiteSpace(userId))
    {
        return Unauthorized();
    }
    
    // Use userId to fetch customer data
    CustomerDto? customer = await _customerService.GetByIdAsync(int.Parse(userId), cancellationToken);
    return Ok(new Response<CustomerDto>(customer, "Profile retrieved successfully"));
}
```

## Best Practices

### 1. **Use [AllowAnonymous] Sparingly**
Only use for endpoints that truly need to be public:
- Health checks
- Public documentation
- Authentication endpoints

### 2. **Don't Expose Detailed Errors**
Authentication errors should be generic:
```csharp
// Good
"Token is missing or invalid"

// Bad
"Token signature verification failed: Invalid key"
```

### 3. **Log Security Events**
All authentication attempts should be logged:
- Successful authentications (debug level)
- Failed authentications (warning level)
- Include context (path, method, user ID if available)

### 4. **Validate Tokens with SSO Service**
Don't rely solely on token parsing. Always validate with the SSO service to ensure:
- Token hasn't been revoked
- Token is still valid
- Token was issued by trusted service

### 5. **Check Expiration Early**
Check token expiration before SSO service call to improve performance.

### 6. **Leverage Caching**
- Validation results are automatically cached until token expiration
- Reduces SSO service load and improves response time
- Cache keys use token hash (not full token) for security
- Cache expiration matches token expiration (automatic cleanup)

### 7. **Monitor Cache Performance**
- Monitor cache hit rates for token validation
- Review cache expiration times
- Ensure cache service is healthy (fallback handles failures)

### 6. **Store User Context Securely**
User information is stored in `HttpContext.Items`, which is:
- Scoped to the current request
- Not persisted across requests
- Thread-safe for the request

### 7. **Use HTTPS Only**
The API enforces HTTPS-only access to protect tokens in transit.

## Troubleshooting

### Issue: All Requests Return 401

**Symptoms:** Even valid tokens are rejected

**Solutions:**
1. Check SSO service configuration
2. Verify `EnableAsmAuthorization` setting
3. Check SSO service endpoint is accessible
4. Review SSO service logs
5. Verify token format matches expected format

### Issue: [AllowAnonymous] Not Working

**Symptoms:** Public endpoints still require authentication

**Solutions:**
1. Verify `[AllowAnonymous]` attribute is from `Microsoft.AspNetCore.Authorization`
2. Check filter order (authentication should run first)
3. Ensure attribute is on the action or controller
4. Verify no other filters are blocking the request

### Issue: User Context Not Available

**Symptoms:** `HttpContext.Items["UserId"]` is null

**Solutions:**
1. Verify authentication passed (check logs)
2. Ensure `IHttpContextAccessor` is injected
3. Check token contains `pid` claim (user ID)
4. Verify `JwtTokenHelper.GetUserId()` returns value

### Issue: Token Validation Too Slow

**Symptoms:** Requests are slow, especially first request

**Solutions:**
1. Check SSO service response time
2. Verify caching is enabled and working (check cache hit rates)
3. Check network connectivity to SSO service
4. Review SSO service logs for performance issues
5. Verify cache service (HybridCache) is healthy
6. Check cache expiration times match token expiration

### Issue: Cache Not Working

**Symptoms:** Every request calls SSO service (no cache hits)

**Solutions:**
1. Verify `ICacheService` is properly registered
2. Check cache key generation (should be 74 characters)
3. Verify cache expiration is calculated correctly
4. Review cache service logs for errors
5. Ensure token expiration is valid (not null)
6. Check if cache service has sufficient memory/storage

### Issue: Expired Tokens Not Rejected

**Symptoms:** Expired tokens are accepted

**Solutions:**
1. Verify `JwtTokenHelper.IsTokenExpired()` is called
2. Check token `exp` claim is present
3. Verify system clock is synchronized
4. Review token expiration logic

## Implementation Details

### Cache Key Generation

The filter generates cache keys using SHA256 hash of the token:

```csharp
private static string GenerateCacheKey(string token)
{
    byte[] tokenBytes = Encoding.UTF8.GetBytes(token);
    byte[] hashBytes = SHA256.HashData(tokenBytes);
    string hashString = Convert.ToHexString(hashBytes).ToLowerInvariant();
    return $"{CacheKeyPrefix}{hashString}";
}
```

**Characteristics:**
- **Fixed length**: Always 74 characters (10 prefix + 64 hex hash)
- **Deterministic**: Same token always produces same cache key
- **Secure**: Token hash prevents token exposure in cache keys
- **Collision-resistant**: SHA256 makes collisions extremely unlikely

### Cache Expiration Calculation

Cache expiration is calculated from token expiration:

```csharp
private static TimeSpan? CalculateCacheExpiration(JwtSecurityToken jwtToken)
{
    DateTimeOffset? tokenExpiration = JwtTokenHelper.GetTokenExpiration(jwtToken);
    if (!tokenExpiration.HasValue)
    {
        return null; // Don't cache if no expiration
    }

    TimeSpan timeUntilExpiration = tokenExpiration.Value - DateTimeOffset.Now;
    
    // Only cache if token has time remaining (at least 1 second)
    return timeUntilExpiration.TotalSeconds > MinimumCacheTimeSeconds 
        ? timeUntilExpiration 
        : null;
}
```

**Behavior:**
- Cache expires when token expires (automatic cleanup)
- Tokens expiring within 1 second are not cached
- Null expiration means no caching (fallback to direct validation)

### Error Handling with Fallback

The filter implements fail-open strategy for cache errors:

```csharp
try
{
    // Try cache-first validation
    return await _cacheService.GetOrCreateAsync(...);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Error during token validation with cache");
    
    // Fallback to direct SSO validation
    try
    {
        return await _ssoService.ValidateTokenAsync(token, cancellationToken);
    }
    catch (Exception fallbackEx)
    {
        _logger.LogError(fallbackEx, "Error during fallback token validation");
        return false; // Fail closed on SSO service error
    }
}
```

**Strategy:**
- Cache errors: Fail-open (fallback to direct SSO validation)
- SSO service errors: Fail-closed (return false, reject authentication)
- Ensures availability even if cache service is unavailable

## Related Documentation

- [JWT Token Helper](../src/WebShop.Util/Security/JwtTokenHelper.cs)
- [SSO Service Implementation](../src/WebShop.Business/Services/SsoService.cs)
- [Standardized Response Model](../src/WebShop.Api/Models/Response.cs)
- [Hybrid Caching Guide](../guides/hybrid-caching.md) (HybridCache implementation and best practices)

## References (Microsoft & industry)

- [Authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/) - Official Microsoft auth guidance
- [JWT Bearer authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn) - JWT middleware (this project uses a custom filter for SSO integration)
- [RFC 7519: JSON Web Token (JWT)](https://datatracker.ietf.org/doc/html/rfc7519) - JWT standard

## Summary

The `JwtTokenAuthenticationFilter` provides:
- ✅ Centralized authentication for all endpoints
- ✅ Security by default (opt-out with `[AllowAnonymous]`)
- ✅ Consistent error responses
- ✅ User context storage
- ✅ Security logging and monitoring
- ✅ Integration with custom SSO service
- ✅ **Token validation caching** (reduces SSO service calls)
- ✅ **Cache-first strategy** (improves response time)
- ✅ **Fail-open error handling** (ensures availability)
- ✅ **Secure cache key generation** (SHA256 hash, not full token)

By using `JwtTokenAuthenticationFilter`, developers can focus on business logic while ensuring all protected endpoints require valid authentication and user information is available throughout the request pipeline. The caching mechanism significantly reduces SSO service load while maintaining security and availability.

