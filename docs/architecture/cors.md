# CORS Configuration Guide

This guide explains the Cross-Origin Resource Sharing (CORS) implementation in the WebShop API, including environment-based policies, configuration options, security considerations, and best practices.

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Environment-Based Policies](#environment-based-policies)
- [Configuration Settings](#configuration-settings)
- [Setting Explanations](#setting-explanations)
- [Environment-Specific Configuration](#environment-specific-configuration)
- [Security Best Practices](#security-best-practices)
- [OpenTelemetry Headers](#opentelemetry-headers)
- [Troubleshooting](#troubleshooting)

## Overview

CORS (Cross-Origin Resource Sharing) is a security mechanism that allows web applications running on one domain to access resources from another domain. The WebShop API implements environment-based CORS policies to provide flexible security configurations for different deployment scenarios.

**Why CORS exists:**

- Browsers enforce the Same-Origin Policy by default, blocking cross-origin requests
- CORS allows APIs to explicitly permit cross-origin requests from trusted domains
- Without CORS, frontend applications on different domains cannot access the API

**Implementation Location:**

- Service registration: `src/WebShop.Api/Extensions/CorsExtensions.cs`
- Configuration model: `src/WebShop.Util/Models/CorsSettings.cs`
- Configuration file: `src/WebShop.Api/appsettings.json` (or environment-specific files)

## Environment-Based Policies

The API uses two CORS policies based on the environment:

### Local Developer Environment

- **Policy Name**: `AllowAll`
- **Behavior**: Allows requests from any origin, with any method, and any headers
- **Configuration**: Automatic, no configuration needed
- **Use Case**: Local development and testing
- **Security**: ⚠️ **Never use in production**

```csharp
// Automatically applied in Development environment
policy.AllowAnyOrigin()
      .AllowAnyMethod()
      .AllowAnyHeader();
```

### Production/Staging/QA/Dev Environments

- **Policy Name**: `Restricted`
- **Behavior**: Configured via `appsettings.json` → `CorsOptions` section
- **Configuration**: Explicit configuration required
- **Use Case**: Deployed environments with specific origin restrictions
- **Security**: ✅ **Production-ready with explicit origin restrictions**

The policy is automatically selected based on the environment:

```csharp
// Policy selection logic
string policyName = app.Environment.IsDevelopment()
    ? "AllowAll"
    : "Restricted";
```

## Configuration Settings

Configure CORS settings in `appsettings.json` (or environment-specific files):

```json
{
  "CorsOptions": {
    "AllowedOrigins": [
      "https://your-production-domain.com",
      "https://www.your-production-domain.com"
    ],
    "AllowedMethods": [
      "GET",
      "POST",
      "PUT",
      "DELETE",
      "PATCH",
      "OPTIONS"
    ],
    "AllowedHeaders": [],
    "AllowCredentials": false,
    "MaxAgeSeconds": 3600
  }
}
```

### Configuration Behavior

The implementation uses smart defaults when configuration arrays are empty:

- **`AllowedOrigins`**: If empty, no origins are allowed (most restrictive)
- **`AllowedMethods`**: If empty, defaults to common methods: `GET`, `POST`, `PUT`, `DELETE`, `PATCH`, `OPTIONS`
- **`AllowedHeaders`**: If empty, allows all headers (`AllowAnyHeader()`)

## Setting Explanations

### `AllowedOrigins` (array of strings)

**Purpose**: List of allowed origin URLs that can make cross-origin requests.

**Configuration:**

```json
"AllowedOrigins": [
  "https://your-production-domain.com",
  "https://www.your-production-domain.com"
]
```

**Requirements:**

- Must include protocol (`https://`) and domain
- Cannot use wildcards (`*`) when `AllowCredentials` is `true`
- Must specify exact origins in production

**Examples:**

- ✅ Valid: `"https://example.com"`, `"https://www.example.com"`
- ❌ Invalid: `"example.com"` (missing protocol), `"*"` (wildcard not allowed with credentials)

**Security Notes:**

- Only include trusted domains
- Never use `*` or `AllowAnyOrigin()` in production
- If empty, no origins are allowed (most restrictive)

**Implementation:**

```csharp
if (corsSettings.AllowedOrigins.Length > 0)
{
    policy.WithOrigins(corsSettings.AllowedOrigins);
}
else
{
    // Default: no origins allowed (most restrictive)
    policy.WithOrigins(Array.Empty<string>());
}
```

### `AllowedMethods` (array of strings)

**Purpose**: HTTP methods allowed in cross-origin requests.

**Configuration:**

```json
"AllowedMethods": [
  "GET",
  "POST",
  "PUT",
  "DELETE",
  "PATCH",
  "OPTIONS"
]
```

**Common Values:**

- `GET` - Retrieve resources
- `POST` - Create resources
- `PUT` - Update resources (full replacement)
- `PATCH` - Partial update resources
- `DELETE` - Delete resources
- `OPTIONS` - Preflight requests (automatically handled)

**Default Behavior:**

- If empty, defaults to: `GET`, `POST`, `PUT`, `DELETE`, `PATCH`, `OPTIONS`

**Security Notes:**

- Only include methods your API actually uses
- `OPTIONS` is required for CORS preflight requests (automatically included)

**Implementation:**

```csharp
if (corsSettings.AllowedMethods.Length > 0)
{
    policy.WithMethods(corsSettings.AllowedMethods);
}
else
{
    // Default: common HTTP methods
    policy.WithMethods("GET", "POST", "PUT", "DELETE", "PATCH", "OPTIONS");
}
```

### `AllowedHeaders` (array of strings)

**Purpose**: HTTP headers that clients are allowed to send in cross-origin requests.

**Configuration:**

```json
"AllowedHeaders": [
  "Content-Type",
  "Authorization",
  "X-Requested-With",
  "api-version"
]
```

**Common Headers:**

- `Content-Type` - Request body content type (e.g., `application/json`)
- `Authorization` - JWT Bearer tokens for authentication
- `X-Requested-With` - Indicates AJAX requests
- `api-version` - API versioning header (alternative to URL segment)

**Default Behavior:**

- If empty, allows all headers (`AllowAnyHeader()`)
- This is less restrictive but convenient for development

**Important Notes:**

- `AllowedHeaders` controls which headers can be **sent** in requests
- Standard browser headers (like `Accept`, `User-Agent`) are always allowed
- `api-version` header is used for API versioning (alternative to URL segment)

**Security Notes:**

- For production, consider specifying only required headers
- Avoid wildcards - be explicit about which headers are allowed
- Current implementation allows all headers when empty (convenient but less secure)

**Implementation:**

```csharp
if (corsSettings.AllowedHeaders.Length > 0)
{
    policy.WithHeaders(corsSettings.AllowedHeaders);
}
else
{
    // Allow all headers when no specific headers are configured
    policy.AllowAnyHeader();
}
```

### `AllowCredentials` (boolean)

**Purpose**: Whether to allow cookies and client certificates in cross-origin requests.

**Configuration:**

```json
"AllowCredentials": false
```

**Default**: `false` (recommended for JWT-based APIs)

**When to Set `true`:**

- Your frontend uses cookie-based authentication
- You need to send/receive cookies (e.g., CSRF tokens, session cookies)
- You use mixed authentication (JWT + cookies)
- You need client certificates

**When to Keep `false`:**

- ✅ **JWT Bearer token authentication** (current setup)
- ✅ APIs that don't use cookies
- ✅ Most REST APIs with token-based auth

**Security Notes:**

- When `true`, you **must** specify exact origins (cannot use wildcards)
- When `true`, you cannot use `AllowAnyOrigin()` - it will throw an exception
- Current setup uses `false` because the API uses JWT Bearer tokens in `Authorization` header (no cookies)

**Important Distinction:**

- `AllowCredentials` does **not** control whether the `Authorization` header can be sent
- The `Authorization` header is controlled by `AllowedHeaders`
- `AllowCredentials` controls whether credentials (cookies, etc.) are included automatically by the browser

**Implementation:**

```csharp
if (corsSettings.AllowCredentials)
{
    policy.AllowCredentials();
}
```

### `MaxAgeSeconds` (integer, optional)

**Purpose**: Maximum time (in seconds) browsers can cache preflight OPTIONS requests.

**Configuration:**

```json
"MaxAgeSeconds": 3600
```

**Default**: If not specified, browsers will make preflight requests for each cross-origin call

**Recommended Values:**

- `3600` (1 hour) - Good balance for production
- `86400` (24 hours) - Maximum caching (use with caution)
- `600` (10 minutes) - Conservative approach

**Range**: Typically 600-86400 seconds (10 minutes to 24 hours)

**How It Works:**

- Browsers send OPTIONS preflight requests before actual cross-origin requests
- `MaxAgeSeconds` tells browsers how long to cache the preflight response
- Reduces the number of preflight requests, improving performance

**Implementation:**

```csharp
if (corsSettings.MaxAgeSeconds.HasValue)
{
    policy.SetPreflightMaxAge(TimeSpan.FromSeconds(corsSettings.MaxAgeSeconds.Value));
}
```

## Environment-Specific Configuration

For different environments, use update configuration in Vault than relying on configuration files:

### Local Development

- Uses `AllowAll` policy automatically
- No configuration needed in `appsettings.Development.json`

### Dev/QA/UAT/Production

- Configure `CorsOptions` in vault
- Must specify exact origins
- Restrict methods and headers appropriately

**Example Structure:**

```
src/WebShop.Api/
├── appsettings.json              # Base configuration
└── appsettings.Development.json  # Local Development overrides
```

## Security Best Practices

1. **Never use `AllowAnyOrigin()` in production**
   - Always specify exact origins
   - Wildcards are a security risk

2. **Use HTTPS only**
   - The API enforces HTTPS-only access
   - CORS origins must use `https://` protocol

3. **Minimize allowed methods**
   - Only include HTTP methods your API actually uses
   - Don't allow `DELETE` if you don't support it

4. **Minimize allowed headers**
   - Only include headers your frontend needs
   - Avoid `AllowAnyHeader()` in production if possible
   - Current implementation allows all headers when empty

5. **Set `AllowCredentials: false` unless needed**
   - Most JWT-based APIs don't need credentials
   - Only enable if you use cookies or client certificates

6. **Use environment-specific configs**
   - Different CORS settings for Dev/QA/UAT/production through Vault
   - Local Development can be permissive, production must be restrictive

7. **Regularly review allowed origins**
   - Remove unused origins
   - Update when domains change
   - Document why each origin is allowed

## OpenTelemetry Headers

For API-to-API communication with distributed tracing, you may want to allow OpenTelemetry headers:

### W3C Trace Context Headers (Recommended)

These headers are automatically handled by OpenTelemetry instrumentation:

- **`traceparent`** - W3C Trace Context header for distributed tracing
- **`tracestate`** - W3C Trace Context header for vendor-specific trace information

### Custom Correlation Headers (Optional)

- **`X-Request-ID`** - Request correlation ID
- **`X-Correlation-ID`** - Additional correlation identifier

### Configuration Example

If you want to explicitly allow OpenTelemetry headers:

```json
{
  "CorsOptions": {
    "AllowedHeaders": [
      "Content-Type",
      "Authorization",
      "X-Requested-With",
      "api-version",
      "traceparent",
      "tracestate",
      "X-Request-ID",
      "X-Correlation-ID"
    ]
  }
}
```

**Note**: With the current implementation, if `AllowedHeaders` is empty, all headers are allowed, so OpenTelemetry headers will work automatically.

## Troubleshooting

### CORS Errors in Browser Console

**Error**: `Access to fetch at 'https://api.example.com' from origin 'https://frontend.example.com' has been blocked by CORS policy`

**Solutions:**

1. Verify the origin is in `AllowedOrigins`
2. Check that the HTTP method is in `AllowedMethods`
3. Ensure required headers are in `AllowedHeaders`
4. Verify `AllowCredentials` setting matches your authentication approach

### Preflight Requests Failing

**Error**: OPTIONS request returns 404 or 405

**Solutions:**

1. Ensure `OPTIONS` is in `AllowedMethods` (it's included by default)
2. Check that CORS middleware is registered before routing
3. Verify the endpoint exists and handles OPTIONS requests

### Authorization Header Not Working

**Issue**: `Authorization` header is blocked

**Solutions:**

1. Add `Authorization` to `AllowedHeaders` (or use empty array to allow all headers)
2. Note: `AllowCredentials` does not control `Authorization` header - that's controlled by `AllowedHeaders`
3. For JWT Bearer tokens, `AllowCredentials: false` is correct

### Wildcard Origins Not Working

**Error**: Cannot use `*` with `AllowCredentials: true`

**Solution**: When `AllowCredentials` is `true`, you must specify exact origins. Cannot use wildcards.

### Development vs Production Behavior

**Issue**: CORS works in development but fails in production

**Solutions:**

1. Verify environment is correctly detected (not Development in production)
2. Check Vault has correct CORS configuration
3. Ensure `AllowedOrigins` includes your production frontend domain
4. Verify HTTPS is used (required for production)

## Implementation Details

### Policy Selection

The CORS policy is automatically selected based on the environment:

```csharp
public static string GetCorsPolicyName(this WebApplication app)
{
    return app.Environment.IsDevelopment()
        ? "AllowAll"
        : "Restricted";
}
```

### Middleware Registration

CORS middleware must be registered early in the pipeline, before routing:

```csharp
app.UseCors(app.GetCorsPolicyName());
```

### Configuration Binding

CORS settings are bound from configuration:

```csharp
CorsSettings corsSettings = new();
configuration.GetSection("CorsOptions").Bind(corsSettings);
```

## Related Documentation

- [API Versioning Guidelines](../standards/api-versioning-guidelines.md) - Using `api-version` header
- [JWT Authentication Filter](jwt-authentication-filter.md) - Authorization header usage
- [OpenTelemetry Implementation](../src/WebShop.Util/OpenTelemetry/) - Distributed tracing headers

## References (Microsoft & industry)

- [Enable Cross-Origin Requests (CORS) in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/cors) - Official Microsoft guidelines (takes precedence)
- [MDN: CORS](https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS) - Cross-Origin Resource Sharing
- [Fetch Standard - CORS](https://fetch.spec.whatwg.org/#http-cors-protocol) - Industry standard

## Summary

The WebShop API implements flexible, environment-based CORS policies that:

- ✅ Allow permissive access in development
- ✅ Enforce strict restrictions in production
- ✅ Support JWT Bearer token authentication
- ✅ Provide smart defaults for common scenarios
- ✅ Follow security best practices

For production deployments, always:

1. Specify exact origins (never use wildcards)
2. Restrict methods and headers appropriately
3. Use HTTPS only
4. Keep `AllowCredentials: false` unless cookies are needed
5. Use environment-specific configuration files
