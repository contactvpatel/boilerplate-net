# HttpClient Factory Implementation Guide

## Overview

This project uses the built-in `IHttpClientFactory` and `HttpClient` from .NET for all external HTTP service communication. The implementation includes a base service class (`HttpServiceBase`), centralized error handling, modern resilience policies using `Microsoft.Extensions.Http.Resilience`, security hardening, and performance optimizations using System.Text.Json source generators.

**Note:** This project uses `Microsoft.Extensions.Http.Resilience` (the modern replacement for the deprecated `Microsoft.Extensions.Http.Polly` package) following Microsoft .NET 10 best practices and guidelines. The standard resilience handler provides five built-in strategies: rate limiter, total timeout, retry, circuit breaker, and attempt timeout.

[← Back to README](../../README.md)

## Table of Contents

- [Why Choose Default HttpClient Over Third-Party Packages](#why-choose-default-httpclient-over-third-party-packages)
- [Architecture & Design](#architecture--design)
- [Security Optimizations](#security-optimizations)
- [Performance Optimizations](#performance-optimizations)
- [Resilience Patterns](#resilience-patterns)
- [Implementation Details](#implementation-details)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Why Choose Default HttpClient Over Third-Party Packages

### Comparison with RestSharp and Other Third-Party Libraries

#### **Built-in HttpClient Advantages:**

1. **No External Dependencies**
   - HttpClient is part of the .NET runtime, reducing attack surface
   - No version conflicts or dependency management overhead
   - Guaranteed compatibility with .NET framework updates

2. **Better Performance**
   - Native implementation optimized for .NET runtime
   - Direct integration with `IHttpClientFactory` for connection pooling
   - Lower memory overhead compared to wrapper libraries

3. **IHttpClientFactory Integration**
   - Built-in support for named clients and typed clients
   - Automatic connection pooling and socket reuse
   - Prevents socket exhaustion issues
   - Lifecycle management handled by DI container

4. **Modern Resilience Integration**
   - First-class support for resilience policies
   - Uses `Microsoft.Extensions.Http.Resilience` (modern replacement for deprecated `Microsoft.Extensions.Http.Polly`)
   - Standard resilience handler with rate limiter, timeout, retry, circuit breaker, and attempt timeout
   - Follows Microsoft .NET 10 best practices and guidelines

5. **System.Text.Json Source Generators**
   - Compile-time JSON serialization for optimal performance
   - No reflection overhead at runtime
   - Type-safe serialization/deserialization

6. **Modern Async/Await Patterns**
   - Native async/await support without wrappers
   - Proper cancellation token support
   - Stream-based content handling

#### **RestSharp Limitations:**

1. **Additional Dependency**
   - Requires external NuGet package
   - Potential security vulnerabilities in third-party code
   - Version compatibility issues

2. **Performance Overhead**
   - Wrapper layer adds overhead
   - Reflection-based serialization (unless configured)
   - Less efficient connection management

3. **Limited Factory Support**
   - Requires manual HttpClient management
   - No built-in integration with `IHttpClientFactory`
   - Higher risk of socket exhaustion

4. **Less Modern**
   - Older API design patterns
   - Limited async/await support in older versions
   - Less aligned with modern .NET practices

#### **Decision Rationale:**

For this project, we chose the built-in `HttpClient` with `IHttpClientFactory` because:

- **Production-Ready**: Battle-tested by Microsoft and the .NET community
- **Performance**: Native implementation with source generators
- **Security**: No external dependencies, reduced attack surface
- **Maintainability**: Standard .NET patterns, easier for team members
- **Flexibility**: Full control over HTTP behavior and policies

## Architecture & Design

### Component Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Controllers (API Layer)                   │
└───────────────────────┬─────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────┐
│        Service Interfaces (Core.Interfaces.Services)       │
│  - ISsoService, IMisService, IAsmService                     │
└───────────────────────┬─────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────┐
│            Service Implementations (Infrastructure)         │
│  - SsoService, MisService, AsmService                        │
│  └─ Inherit from HttpServiceBase                            │
└───────────────────────┬─────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────┐
│                    HttpServiceBase                           │
│  - Common HTTP operations (GET, POST)                       │
│  - Error handling via HttpErrorHandler                      │
│  - JSON serialization with source generators                │
│  - Request size validation                                  │
└───────────────────────┬─────────────────────────────────────┘
                        │
┌───────────────────────▼─────────────────────────────────────┐
│              IHttpClientFactory (DI Container)              │
│  - Named HttpClient instances                                │
│  - Polly resilience policies (retry + circuit breaker)     │
│  - Service-level header configuration                       │
└─────────────────────────────────────────────────────────────┘
```

### Key Components

1. **HttpServiceBase** (`src/WebShop.Infrastructure/Helpers/HttpServiceBase.cs`)
   - Abstract base class for all HTTP service implementations
   - Provides common methods: `GetAsync<T>`, `GetCollectionAsync<T>`, `PostAsync<TRequest, TResponse>`
   - Handles JSON serialization, error handling, and request validation

2. **HttpErrorHandler** (`src/WebShop.Infrastructure/Helpers/HttpErrorHandler.cs`)
   - Centralized error handling and logging
   - Sanitizes sensitive data from error messages
   - Throws appropriate exceptions with context

3. **HttpClientExtensions** (`src/WebShop.Infrastructure/Helpers/HttpClientExtensions.cs`)
   - Extension methods for common operations
   - Thread-safe header manipulation on `HttpRequestMessage`
   - URL manipulation helpers

4. **SensitiveDataSanitizer** (`src/WebShop.Infrastructure/Helpers/SensitiveDataSanitizer.cs`)
   - Removes tokens, passwords, and sensitive data from logs
   - Prevents credential leakage in error messages

5. **JsonContext** (`src/WebShop.Infrastructure/Helpers/JsonContext.cs`)
   - System.Text.Json source generator context
   - Compile-time JSON serialization for optimal performance

## Security Optimizations

### 1. Request Size Limits

**Implementation:**

- **Outgoing Requests (HttpClient)**: Maximum request body size: 1MB (configurable via `HttpResilienceOptions.MaxRequestSizeBytes`)
  - Validated before sending POST requests in `HttpServiceBase`
  - Prevents DoS attacks from large payloads sent to external services
  
- **Incoming Requests (API)**: Request size limits enforced at multiple layers:
  - **Kestrel Level**: `MaxRequestBodySize`, `MaxRequestHeadersTotalSize`, `MaxRequestHeaderCount` (applies automatically to ALL requests)
  - **FormOptions Level**: `MultipartBodyLengthLimit`, `ValueLengthLimit`, `ValueCountLimit` (for form parsing)
  - All configured via `HttpResilienceOptions` in `appsettings.json` (no hardcoded values)
  - When exceeded, users receive standardized JSON error response (413 Request Entity Too Large)

**Code Example (Outgoing Requests):**

```csharp
// In HttpServiceBase.PostAsync
int contentSize = Encoding.UTF8.GetByteCount(jsonContent);
if (contentSize > ResilienceOptions.MaxRequestSizeBytes)
{
    throw new ArgumentException(
        $"Request body size ({contentSize} bytes) exceeds maximum allowed size ({ResilienceOptions.MaxRequestSizeBytes} bytes).", 
        nameof(request));
}
```

**Configuration (appsettings.json):**

```json
{
  "HttpResilienceOptions": {
    "MaxRequestSizeBytes": 1048576,
    "MaxRequestHeadersTotalSize": 32768,
    "MaxRequestHeaderCount": 100,
    "MaxFormValueCount": 1024
  }
}
```

**Note**: Kestrel limits apply automatically to all incoming requests at the server level. When a request exceeds the limit, Kestrel throws `BadHttpRequestException` (413), which is caught by `ExceptionHandlingMiddleware` to provide a proper JSON error response.

### 2. Sensitive Data Sanitization

**Implementation:**

- All error messages and logs are sanitized before output
- Removes JWT tokens, passwords, API keys, credit cards, SSNs
- Masks tokens (keeps first 4 and last 4 characters)

**Code Example:**

```csharp
// In HttpErrorHandler
string sanitizedErrorContent = SensitiveDataSanitizer.Sanitize(errorContent);
logger.LogError("{Message}. Response: {ErrorContent}", logMessage, sanitizedErrorContent);
```

**Sanitization Patterns:**

- JWT/Bearer tokens: `abc123...xyz789`
- Passwords: `password: ***`
- Credit cards: `****-****-****-****`
- SSN: `***-**-****`

### 3. HTTP Header Injection Prevention

**Implementation:**

- Validates header names and values for control characters
- Prevents CRLF injection attacks
- Throws `ArgumentException` for invalid headers

**Code Example:**

```csharp
// In HttpClientExtensions.AddHeader
if (name.Contains('\r') || name.Contains('\n') || value.Contains('\r') || value.Contains('\n'))
{
    throw new ArgumentException("Header name or value contains invalid characters (CRLF injection attempt detected).", nameof(name));
}
```

### 4. Error Response Size Limits

**Implementation:**

- Limits error content reading to 10KB
- Prevents DoS attacks from large error responses
- Truncates content if exceeds limit

**Code Example:**

```csharp
// In HttpErrorHandler
private const int MaxErrorContentSize = 10 * 1024; // 10KB
// Content reading is limited to this size
```

### 5. Thread-Safe Header Configuration

**Implementation:**

- Uses `HttpRequestMessage.Headers` instead of `HttpClient.DefaultRequestHeaders`
- Prevents race conditions in multi-threaded scenarios
- Per-request header configuration

**Code Example:**

```csharp
// Thread-safe: Per-request headers
using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
request.SetBearerToken(token); // Sets header on request, not client
```

### 6. HttpClient Timeout Configuration

**Implementation:**

- Explicit timeout configuration (configurable per service)
- Prevents hanging requests
- Configurable via service options (`TimeoutSeconds` property)

**Code Example:**

```csharp
// In DependencyInjection.RegisterHttpClient
client.Timeout = TimeSpan.FromSeconds(timeoutSeconds); // From service options
```

### 7. URL Validation (SSRF Protection)

**Implementation:**

- Validates all external service URLs before use
- Only allows HTTPS URLs
- Blocks localhost and private IP ranges (RFC 1918)
- Prevents Server-Side Request Forgery (SSRF) attacks

**Code Example:**

```csharp
// In DependencyInjection.ValidateAndSetBaseAddress
if (!UrlValidator.IsValidExternalUrl(url, out Uri? uri))
{
    throw new InvalidOperationException(
        $"Invalid {serviceName} service URL: {url}. URL must be HTTPS and cannot be localhost or private IP.");
}
client.BaseAddress = uri;
```

**Validation Rules:**

- Must be HTTPS (not HTTP)
- Cannot be localhost (127.0.0.1, ::1)
- Cannot be private IP ranges (192.168.x.x, 10.x.x.x, 172.16-31.x.x)
- Cannot be link-local addresses (169.254.x.x)

### 8. Response Size Limits

**Implementation:**

- Maximum response body size: 10MB (configurable via `HttpResilienceOptions.MaxResponseSizeBytes`)
- Validated before deserialization
- Prevents DoS attacks from large responses

**Code Example:**

```csharp
// In HttpServiceBase - before deserializing response
long? contentLength = response.Content.Headers.ContentLength;
if (contentLength.HasValue && contentLength.Value > ResilienceOptions.MaxResponseSizeBytes)
{
    throw new HttpRequestException(
        $"Response body size ({contentLength} bytes) exceeds maximum allowed size ({ResilienceOptions.MaxResponseSizeBytes} bytes).");
}
```

## Performance Optimizations

### 1. System.Text.Json Source Generators

**Implementation:**

- Uses `JsonContext` with source generation attributes
- Compile-time code generation for serialization
- No reflection overhead at runtime
- Type-safe JSON operations

**Code Example:**

```csharp
// JsonContext.cs - Source generator context
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    WriteIndented = false)]
[JsonSerializable(typeof(SsoAuthResponse))]
[JsonSerializable(typeof(DepartmentModel))]
// ... more types
public partial class JsonContext : JsonSerializerContext
{
}

// Usage in HttpServiceBase
JsonOptions = JsonContext.Default.Options; // Pre-compiled options
```

**Benefits:**

- **50-80% faster** serialization compared to reflection-based
- **Reduced memory allocations**
- **Better startup performance**
- **Type safety at compile time**

### 2. IHttpClientFactory Connection Pooling

**Implementation:**

- Named HttpClient instances via factory
- Automatic connection pooling and socket reuse
- Prevents socket exhaustion
- Lifecycle managed by DI container
- Configurable connection pool limits

**Code Example:**

```csharp
// In DependencyInjection.RegisterHttpClient
services.AddHttpClient(clientName, (serviceProvider, client) =>
{
    ValidateAndSetBaseAddress(client, baseUrl, clientName, serviceProvider);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
    configureHeaders?.Invoke(client);
})
.ConfigurePrimaryHttpMessageHandler(() => CreateHttpMessageHandler(resilienceOptions))
.AddPolicyHandler(...);
```

**Connection Pool Configuration:**

```csharp
// In DependencyInjection.CreateHttpMessageHandler
return new SocketsHttpHandler
{
    MaxConnectionsPerServer = resilienceOptions.MaxConnectionsPerServer, // Default: 10
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
    PooledConnectionLifetime = TimeSpan.FromMinutes(10)
};
```

**Benefits:**

- **Reuses TCP connections** across requests
- **Reduces connection overhead**
- **Prevents socket exhaustion**
- **Better resource management**
- **Configurable for high-concurrency scenarios**

### 3. Proper Resource Disposal

**Implementation:**

- Uses `using` statements for `HttpClient` and `HttpResponseMessage`
- Ensures proper disposal of network resources
- Prevents memory leaks

**Code Example:**

```csharp
using HttpClient httpClient = CreateHttpClient();
using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
```

### 4. Efficient JSON Serialization

**Implementation:**

- Single serialization per request
- Reuses `JsonSerializerOptions` instance
- Avoids repeated option creation

## Resilience Patterns

This project implements resilience patterns using `Microsoft.Extensions.Http.Resilience` for HTTP clients. For comprehensive documentation on resilience patterns, strategies, and best practices, see the [Resilience Patterns Guide](resilience.md).

### HTTP Client Resilience Implementation

The HTTP client resilience is configured using the standard resilience handler:

```csharp
.AddStandardResilienceHandler(options =>
{
    // Configure total timeout
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(timeoutSeconds);

    // Configure retry strategy
    options.Retry.MaxRetryAttempts = resilienceOptions.MaxRetryAttempts;
    options.Retry.Delay = TimeSpan.FromSeconds(resilienceOptions.RetryBaseDelaySeconds);

    // Configure circuit breaker strategy
    options.CircuitBreaker.FailureRatio = 0.1; // 10% failure ratio
    options.CircuitBreaker.MinimumThroughput = resilienceOptions.CircuitBreakerMinimumThroughput;
    options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(resilienceOptions.CircuitBreakerBreakDurationSeconds);
});
```

### Standard Resilience Handler Pipeline

The `AddStandardResilienceHandler()` automatically chains five resilience strategies in the correct order:

1. **Rate Limiter** (outermost) - Limits concurrent requests (1000 permits, queue: 0)
2. **Total Timeout** - Overall timeout for entire request including retries
3. **Retry** - Retries on transient errors with exponential backoff
4. **Circuit Breaker** - Opens circuit after too many failures
5. **Attempt Timeout** (innermost) - Timeout for each individual attempt (10s)

For detailed information on resilience patterns, configuration, and best practices, see the [Resilience Patterns Guide](resilience.md).

## Implementation Details

### Creating a New HTTP Service

1. **Create Service Interface** (in `WebShop.Core.Interfaces.Services`):

```csharp
namespace WebShop.Core.Interfaces.Services;

public interface IMyService
{
    Task<MyModel?> GetDataAsync(string id, CancellationToken cancellationToken = default);
    Task<bool> CreateDataAsync(CreateMyModel model, CancellationToken cancellationToken = default);
}
```

1. **Create Service Implementation** (in `WebShop.Infrastructure.Services.External`):

```csharp
namespace WebShop.Infrastructure.Services.External;

public class MyService(
    IHttpClientFactory httpClientFactory,
    IOptions<MyServiceOptions> options,
    ILogger<MyService> logger,
    IOptions<HttpResilienceOptions> resilienceOptions) 
    : HttpServiceBase(httpClientFactory, logger, resilienceOptions), IMyService
{
    private readonly MyServiceOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    protected override string HttpClientName => "MyService";

    public async Task<MyModel?> GetDataAsync(string id, CancellationToken cancellationToken = default)
    {
        string endpoint = $"{_options.Endpoint.GetData}?id={id}";
        return await GetAsync<MyModel>(endpoint, cancellationToken);
    }

    public async Task<bool> CreateDataAsync(CreateMyModel model, CancellationToken cancellationToken = default)
    {
        string endpoint = _options.Endpoint.CreateData;
        return await PostAsync(endpoint, model, cancellationToken);
    }
}
```

1. **Register HttpClient** (in `DependencyInjection.ConfigureHttpClients`):

```csharp
MyServiceOptions myServiceOptions = new();
configuration.GetSection("MyService").Bind(myServiceOptions);

HttpResilienceOptions resilienceOptions = new();
configuration.GetSection("HttpResilienceOptions").Bind(resilienceOptions);

// Use the centralized RegisterHttpClient helper method (follows DRY/SOLID principles)
RegisterHttpClient(services, "MyService", myServiceOptions.Url, myServiceOptions.TimeoutSeconds, 
    resilienceOptions, client =>
    {
        // Thread-safe: Set service-level headers at HttpClient factory level
        if (!string.IsNullOrWhiteSpace(myServiceOptions.ApiKey))
        {
            client.DefaultRequestHeaders.Add("X-API-Key", myServiceOptions.ApiKey);
        }
    });
```

**Note:** The `RegisterHttpClient` method automatically:

- Validates the URL (SSRF protection)
- Configures connection pooling
- Sets up retry and circuit breaker policies with logging
- Applies service-specific headers via the `configureHeaders` delegate

1. **Register Service** (in `DependencyInjection.AddInfrastructure`):

```csharp
// Register the service implementation
services.AddScoped<WebShop.Core.Interfaces.Services.IMyService, MyService>();
```

**Note:** Service interfaces are located in `WebShop.Core.Interfaces.Services` namespace, while base interfaces (like `IRepository`, `ICacheService`, `IUserContext`) are in `WebShop.Core.Interfaces.Base` namespace.

1. **Add Configuration** (in `appsettings.json`):

```json
{
  "MyService": {
    "Url": "https://api.example.com/",
    "TimeoutSeconds": 30,
    "ApiKey": "your-api-key",
    "Endpoint": {
      "GetData": "data/get",
      "CreateData": "data/create"
    }
  },
  "HttpResilienceOptions": {
    "MaxRequestSizeBytes": 1048576,
    "MaxResponseSizeBytes": 10485760,
    "MaxConnectionsPerServer": 10,
    "RetryCount": 3,
    "RetryDelaySeconds": 2,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerDurationSeconds": 30
  }
}
```

### Per-Request Authentication

For requests requiring Bearer tokens or custom headers:

```csharp
// In service method
public async Task<MyModel?> GetAuthenticatedDataAsync(
    string id, 
    string bearerToken, 
    CancellationToken cancellationToken = default)
{
    string endpoint = $"{_options.Endpoint.GetData}?id={id}";
    
    // Thread-safe: Configure headers on HttpRequestMessage
    return await GetAsync<MyModel>(
        endpoint,
        request => request.SetBearerToken(bearerToken),
        cancellationToken);
}
```

### Error Handling

All errors are automatically handled by `HttpErrorHandler`:

```csharp
// In HttpServiceBase - errors are automatically:
// 1. Logged with sanitized content
// 2. Thrown as appropriate exceptions
// 3. Retried if transient (via Polly)
// 4. Circuit breaker opened if persistent failures
```

**Exception Types:**

- `HttpRequestException`: HTTP errors (400, 404, 500, etc.)
- `TaskCanceledException`: Timeout or cancellation
- `JsonException`: JSON deserialization errors
- `ArgumentException`: Request validation errors (e.g., size limit)

## Configuration

### HttpResilienceOptions

Configure resilience policies in `appsettings.json`:

```json
{
  "HttpResilienceOptions": {
    "MaxRequestSizeBytes": 1048576,
    "MaxResponseSizeBytes": 10485760,
    "MaxConnectionsPerServer": 10,
    "RetryCount": 3,
    "RetryDelaySeconds": 2,
    "CircuitBreakerFailureThreshold": 5,
    "CircuitBreakerDurationSeconds": 30,
    "MaxRequestHeadersTotalSize": 32768,
    "MaxRequestHeaderCount": 100,
    "MaxFormValueCount": 1024
  }
}
```

**Configuration Options:**

| Property | Default | Description |
|----------|---------|------------|
| `MaxRequestSizeBytes` | 1,048,576 (1MB) | Maximum request body size (used for Kestrel, FormOptions, and HttpClient validation) |
| `MaxResponseSizeBytes` | 10,485,760 (10MB) | Maximum response body size to prevent DoS |
| `MaxConnectionsPerServer` | 10 | Maximum concurrent connections per server for connection pooling |
| `MaxRequestHeadersTotalSize` | 32,768 (32KB) | Maximum total size of request headers (Kestrel level) |
| `MaxRequestHeaderCount` | 100 | Maximum number of request headers (Kestrel level) |
| `MaxFormValueCount` | 1,024 | Maximum number of form values (FormOptions level) |
| `MaxRetryAttempts` (or `RetryCount`) | 3 | Maximum number of retry attempts for transient failures |
| `RetryBaseDelaySeconds` (or `RetryDelaySeconds`) | 2 | Base delay in seconds for exponential backoff |
| `CircuitBreakerMinimumThroughput` (or `CircuitBreakerFailureThreshold`) | 5 | Minimum number of requests required before circuit breaker can evaluate failures |
| `CircuitBreakerBreakDurationSeconds` (or `CircuitBreakerDurationSeconds`) | 30 | Duration in seconds that circuit breaker stays open |

**Note:** Property names support backward compatibility. You can use either the new names (aligned with Microsoft's API) or the old names in `appsettings.json`. See [Resilience Patterns Guide](resilience.md) for detailed configuration options and best practices.

**Service-Specific Options:**

Each service can configure its own timeout:

| Property | Default | Description |
|----------|---------|------------|
| `TimeoutSeconds` | 30 | Request timeout in seconds (per service) |

**Environment-Specific Configuration:**

```json
// appsettings.Development.json
{
  "HttpResilienceOptions": {
    "RetryCount": 2,
    "CircuitBreakerFailureThreshold": 3
  }
}

// appsettings.Production.json
{
  "HttpResilienceOptions": {
    "RetryCount": 5,
    "CircuitBreakerFailureThreshold": 10,
    "CircuitBreakerDurationSeconds": 60
  }
}
```

### Service-Specific Options

Each service has its own configuration section:

```json
{
  "SsoService": {
    "Url": "https://api.example.com/sso/",
    "TimeoutSeconds": 30,
    "Endpoint": {
      "ValidateToken": "user/validate/token",
      "RenewToken": "user/renewtoken",
      "Logout": "user/logout"
    }
  },
  "MisService": {
    "Url": "https://api.example.com/mis/",
    "TimeoutSeconds": 30,
    "Headers": {
      "AuthAppId": "app-id",
      "AuthAppSecret": "app-secret"
    },
    "Endpoint": {
      "Department": "Department",
      "Role": "Role"
    }
  }
}
```

## Usage Examples

### Basic GET Request

```csharp
public async Task<DepartmentModel?> GetDepartmentAsync(int id, CancellationToken cancellationToken = default)
{
    string endpoint = $"{_options.Endpoint.Department}?id={id}";
    return await GetAsync<DepartmentModel>(endpoint, cancellationToken);
}
```

### GET Collection

```csharp
public async Task<IEnumerable<DepartmentModel>> GetAllDepartmentsAsync(
    int divisionId, 
    CancellationToken cancellationToken = default)
{
    string endpoint = $"{_options.Endpoint.Department}?divisionId={divisionId}";
    return await GetCollectionAsync<DepartmentModel>(endpoint, cancellationToken);
}
```

### POST with Request Body

```csharp
public async Task<CreateResponse?> CreateDepartmentAsync(
    CreateDepartmentModel model, 
    CancellationToken cancellationToken = default)
{
    string endpoint = _options.Endpoint.Department;
    return await PostAsync<CreateDepartmentModel, CreateResponse>(
        endpoint, 
        model, 
        cancellationToken);
}
```

### POST with Authentication

```csharp
public async Task<bool> RenewTokenAsync(
    string accessToken, 
    string refreshToken, 
    CancellationToken cancellationToken = default)
{
    string endpoint = _options.Endpoint.RenewToken;
    var request = new { refreshToken };
    
    return await PostAsync<object, SsoAuthResponse>(
        endpoint,
        request,
        httpRequest => httpRequest.SetBearerToken(accessToken),
        cancellationToken) != null;
}
```

### POST Without Response Body

```csharp
public async Task<bool> LogoutAsync(string token, CancellationToken cancellationToken = default)
{
    string endpoint = _options.Endpoint.Logout;
    return await PostAsync(
        endpoint,
        request => request.SetBearerToken(token),
        cancellationToken);
}
```

## Best Practices

### 1. Always Use IHttpClientFactory

❌ **Don't:**

```csharp
using (var client = new HttpClient())
{
    // Socket exhaustion risk
}
```

✅ **Do:**

```csharp
// Inject IHttpClientFactory and use named clients
protected override string HttpClientName => "MyService";
```

### 2. Use HttpRequestMessage for Per-Request Headers

❌ **Don't:**

```csharp
httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
// Thread-safety issue in concurrent scenarios
```

✅ **Do:**

```csharp
using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
request.SetBearerToken(token); // Thread-safe per-request header
```

### 3. Always Use Cancellation Tokens

✅ **Do:**

```csharp
public async Task<MyModel?> GetDataAsync(string id, CancellationToken cancellationToken = default)
{
    return await GetAsync<MyModel>(endpoint, cancellationToken);
}
```

### 4. Register Types in JsonContext

✅ **Do:**

```csharp
// Add new types to JsonContext.cs for source generator performance
[JsonSerializable(typeof(MyModel))]
[JsonSerializable(typeof(IEnumerable<MyModel>))]
public partial class JsonContext : JsonSerializerContext
{
}
```

### 5. Configure Service-Level Headers at Factory Level

✅ **Do:**

```csharp
services.AddHttpClient("MyService", client =>
{
    // Service-level headers (applied to all requests)
    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
});
```

### 6. Use Appropriate Logging Levels

✅ **Do:**

```csharp
Logger.LogInformation("Fetching data for ID: {Id}", id); // Normal operation
Logger.LogWarning("Request failed: {Message}", message); // Recoverable error
Logger.LogError(exception, "Unexpected error: {Message}", message); // Exception
```

### 7. Validate Request Size Before Sending

✅ **Do:**

```csharp
// HttpServiceBase automatically validates request size
// Custom validation can be added in service methods if needed
if (model.Data.Length > MaxCustomSize)
{
    throw new ArgumentException("Data too large");
}
```

## Troubleshooting

### Issue: Socket Exhaustion

**Symptoms:**

- `SocketException: Address already in use`
- Slow or hanging requests
- High number of TIME_WAIT connections

**Solution:**

- Ensure using `IHttpClientFactory` (not direct `HttpClient` instantiation)
- Check that services are registered with named clients
- Verify proper disposal of `HttpClient` instances

### Issue: Circuit Breaker Always Open

**Symptoms:**

- All requests fail immediately with circuit breaker exceptions
- Service appears down even when it's up

**Solution:**

- Check `CircuitBreakerMinimumThroughput` (or `CircuitBreakerFailureThreshold`) in configuration
- Verify external service is actually responding
- Review logs for actual error responses
- Temporarily increase `CircuitBreakerBreakDurationSeconds` (or `CircuitBreakerDurationSeconds`) for testing

### Issue: Retries Not Working

**Symptoms:**

- Requests fail immediately without retries
- No exponential backoff observed

**Solution:**

- Verify error is a transient error (5xx, 408, 429)
- Check `MaxRetryAttempts` (or `RetryCount`) configuration
- Ensure Polly policies are registered: `.AddPolicyHandler(resiliencePolicy)`
- Review logs for retry attempts

### Issue: JSON Deserialization Errors

**Symptoms:**

- `JsonException` on deserialization
- Missing properties in response models

**Solution:**

- Add type to `JsonContext` for source generator
- Verify response model matches API response
- Check `PropertyNameCaseInsensitive` setting
- Review actual API response format

### Issue: Request Size Limit Exceeded

**Symptoms:**

- **Outgoing Requests**: `ArgumentException: Request body size exceeds maximum`
- **Incoming Requests**: `413 Request Entity Too Large` with JSON error response

**Solution:**

- **For Outgoing Requests**: Increase `MaxRequestSizeBytes` in `HttpResilienceOptions` or optimize request payload size
- **For Incoming Requests**:
  - Increase `MaxRequestSizeBytes` in `HttpResilienceOptions` (affects both Kestrel and FormOptions)
  - Users will receive standardized JSON error response:

    ```json
    {
      "succeeded": false,
      "message": "Request body size exceeds the maximum allowed limit...",
      "errors": [{
        "errorId": "...",
        "statusCode": 413,
        "message": "..."
      }]
    }
    ```

  - Kestrel limits apply automatically to all requests - no code changes needed
- Consider pagination or chunking for large data

### Issue: Timeout Errors

**Symptoms:**

- `TaskCanceledException` on requests
- Requests taking longer than expected

**Solution:**

- Increase `HttpClient.Timeout` in service configuration
- Check network connectivity
- Review external service performance
- Consider increasing retry delays

### Issue: Sensitive Data in Logs

**Symptoms:**

- Tokens or passwords visible in logs

**Solution:**

- Verify `SensitiveDataSanitizer` is being used
- Check that `HttpErrorHandler` sanitizes all error content
- Review log output format

## Related Documentation

- [HttpClient Lifecycle & Connection Pooling](httpclient-lifecycle.md) - Detailed explanation of HttpClient lifecycle, connection pooling, and socket exhaustion prevention
- [ValidationFilter Guide](validation-filter.md) - Request validation
- [JWT Authentication Filter Guide](jwt-authentication-filter.md) - Authentication
- [Caching Guide](caching.md) - Caching strategies
- [DbUp Migrations Guide](dbup-migrations.md) - Database migrations

## Summary

This implementation provides:

✅ **Security**: Request/response size limits, URL validation (SSRF protection), header validation, sensitive data sanitization  
✅ **Performance**: Source generators, connection pooling (configurable), efficient serialization  
✅ **Resilience**: Automatic retries with logging, circuit breaker pattern with state logging, configurable policies  
✅ **Maintainability**: DRY/SOLID principles, centralized error handling, consistent patterns, clear structure  
✅ **Observability**: Structured logging for retries and circuit breaker state changes, health checks for external services  
✅ **Production-Ready**: Thread-safe, properly disposed resources, comprehensive logging, configurable timeouts  
✅ **Socket Exhaustion Prevention**: Shared HttpMessageHandler instances with configurable connection pooling

The built-in `HttpClient` with `IHttpClientFactory` provides all necessary features without external dependencies, making it the optimal choice for production applications.

### Architecture Highlights

- **DRY Principle**: Common HttpClient configuration centralized in `RegisterHttpClient` helper method
- **SOLID Principles**: Single responsibility methods (`ValidateAndSetBaseAddress`, `CreateHttpMessageHandler`, etc.)
- **Security First**: URL validation prevents SSRF attacks, response size limits prevent DoS
- **Production Observability**: Comprehensive logging for retries and circuit breaker state changes
- **Health Checks**: External service health checks integrated into application health endpoints

### Connection Pooling & Lifecycle

For detailed information about HttpClient lifecycle, connection pooling, and socket exhaustion prevention, see the [HttpClient Lifecycle Guide](httpclient-lifecycle.md).
