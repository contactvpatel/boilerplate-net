# HttpClient Lifecycle, Connection Pooling, and Socket Exhaustion Prevention

## Overview

This document explains how `IHttpClientFactory` manages HttpClient instances, implements connection pooling, and prevents socket exhaustion in the WebShop API.

## Table of Contents

- [HttpClient Lifecycle](#httpclient-lifecycle)
- [Connection Pooling](#connection-pooling)
- [Socket Exhaustion Prevention](#socket-exhaustion-prevention)
- [How It Works in Our Implementation](#how-it-works-in-our-implementation)
- [Best Practices](#best-practices)
- [Monitoring and Troubleshooting](#monitoring-and-troubleshooting)

## HttpClient Lifecycle

### The Problem with Direct HttpClient Instantiation

❌ **Bad Practice:**
```csharp
// DON'T DO THIS - Causes socket exhaustion!
public class BadService
{
    public async Task<string> GetDataAsync()
    {
        using (var client = new HttpClient())  // Creates new socket each time
        {
            return await client.GetStringAsync("https://api.example.com/data");
        }
    }
}
```

**Problems:**
- Each `new HttpClient()` creates a new underlying `HttpClientHandler`
- Each handler creates its own connection pool
- Sockets are not immediately released (TIME_WAIT state)
- After many requests, you run out of available sockets
- Results in `SocketException: Address already in use`

### How IHttpClientFactory Solves This

✅ **Correct Approach:**
```csharp
// In DependencyInjection.cs
services.AddHttpClient("SsoService", client =>
{
    client.BaseAddress = new Uri("https://api.example.com/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

// In Service
public class SsoService : HttpServiceBase
{
    protected override string HttpClientName => "SsoService";
    
    public async Task<string> GetDataAsync()
    {
        HttpClient client = HttpClientFactory.CreateClient(HttpClientName);
        // Use client - don't dispose it!
        return await client.GetStringAsync("endpoint");
    }
}
```

### Lifecycle Details

1. **Factory Registration (Application Startup)**
   ```csharp
   // Happens once at startup
   services.AddHttpClient("SsoService", client => { /* configuration */ });
   ```
   - Registers a named HttpClient configuration
   - Creates an internal `HttpClientHandler` pool
   - Handler is **singleton** and **reused** across all requests

2. **Client Creation (Per Request)**
   ```csharp
   // Called each time you need an HttpClient
   HttpClient client = HttpClientFactory.CreateClient("SsoService");
   ```
   - Factory returns a **new HttpClient instance** (lightweight wrapper)
   - But it **shares the underlying HttpMessageHandler** from the pool
   - HttpClient instance can be disposed (it's just a wrapper)
   - The underlying handler and connections are **reused**

3. **Request Execution**
   ```csharp
   using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
   using HttpResponseMessage response = await client.SendAsync(request);
   ```
   - Uses connection from the shared pool
   - Connection is reused if available
   - New connection created only if needed

4. **Cleanup**
   - HttpClient wrapper can be disposed (doesn't affect pool)
   - HttpMessageHandler stays alive and reuses connections
   - Connections are kept alive for reuse (HTTP keep-alive)
   - Idle connections are closed after timeout

### Lifecycle Diagram

```
Application Startup
    ↓
Register Named Client: AddHttpClient("SsoService")
    ↓
Create HttpMessageHandler Pool (Singleton)
    ├─ Handler 1 (for "SsoService")
    ├─ Handler 2 (for "MisService")
    └─ Handler 3 (for "AsmService")
    ↓
Application Running
    ↓
Request 1: CreateClient("SsoService")
    ├─ Get HttpClient wrapper (new instance)
    ├─ Attach to Handler 1 (shared)
    ├─ Execute request (reuse or create connection)
    └─ Dispose HttpClient wrapper (handler stays alive)
    ↓
Request 2: CreateClient("SsoService")
    ├─ Get HttpClient wrapper (new instance)
    ├─ Attach to Handler 1 (same handler!)
    ├─ Execute request (reuse existing connection!)
    └─ Dispose HttpClient wrapper
    ↓
... (connections reused across requests)
```

## Connection Pooling

### What is Connection Pooling?

Connection pooling is the practice of **reusing TCP connections** across multiple HTTP requests instead of creating a new connection for each request.

### Benefits

1. **Performance**: Reusing connections is much faster than creating new ones
   - New connection: ~100-300ms (DNS lookup, TCP handshake, TLS negotiation)
   - Reused connection: ~1-5ms (just send data)

2. **Resource Efficiency**: Fewer sockets and less memory
   - Without pooling: 1000 requests = 1000 sockets
   - With pooling: 1000 requests = ~10-50 sockets (depending on concurrency)

3. **Scalability**: Can handle more requests with fewer resources

### How IHttpClientFactory Implements Connection Pooling

**Under the Hood:**

1. **HttpMessageHandler Pool**
   - Each named client gets its own `HttpMessageHandler` instance
   - Handler is **singleton** and **thread-safe**
   - All HttpClient instances for the same name share the same handler

2. **Connection Pool per Handler**
   - Each `HttpMessageHandler` maintains its own connection pool
   - Connections are keyed by: `(Scheme, Host, Port)`
   - Example: `https://api.example.com:443` = one pool

3. **Connection Reuse**
   - When a request is made, handler checks for available connection
   - If connection exists and is idle → **reuse it**
   - If no connection → create new one
   - If pool is full → wait or create additional connection

4. **Connection Limits**
   - Default: **2 connections per host** (can be configured)
   - Can be increased via `ServicePointManager` or `SocketsHttpHandler`

### Connection Pooling in Our Implementation

```csharp
// In DependencyInjection.cs
services.AddHttpClient("SsoService", client =>
{
    client.BaseAddress = new Uri("https://api.uat.bapsapps.org/sso/api/");
    // This creates ONE HttpMessageHandler for "SsoService"
    // All requests using "SsoService" share this handler and its connection pool
});

// In HttpServiceBase
protected virtual HttpClient CreateHttpClient()
{
    // Each call returns a NEW HttpClient wrapper
    // But all wrappers share the SAME HttpMessageHandler
    return HttpClientFactory.CreateClient(HttpClientName);
}
```

**What Happens:**
1. First request to SSO service → Creates connection to `api.uat.bapsapps.org:443`
2. Second request to SSO service → **Reuses the same connection** (if idle)
3. Third request (while first two are in use) → Creates second connection
4. After requests complete → Connections stay open for reuse (keep-alive)

### Connection Pool Configuration

Connection pool limits are configured via `HttpResilienceOptions`:

```csharp
// In DependencyInjection.CreateHttpMessageHandler
return new SocketsHttpHandler
{
    MaxConnectionsPerServer = resilienceOptions.MaxConnectionsPerServer, // Configurable, default: 10
    PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
    PooledConnectionLifetime = TimeSpan.FromMinutes(10)
};
```

**Configuration in appsettings.json:**
```json
{
  "HttpResilienceOptions": {
    "MaxConnectionsPerServer": 10
  }
}
```

**Default Values:**
- `MaxConnectionsPerServer`: 10 (configurable, increased from default 2 for better concurrency)
- `PooledConnectionIdleTimeout`: 2 minutes (idle connections closed after this)
- `PooledConnectionLifetime`: 10 minutes (connections can be reused within this lifetime)

## Socket Exhaustion Prevention

### What is Socket Exhaustion?

Socket exhaustion occurs when your application runs out of available TCP sockets. This happens when:

1. **Too many sockets created** (one per HttpClient instance)
2. **Sockets not released quickly** (TIME_WAIT state lasts ~2-4 minutes)
3. **No connection reuse** (new socket for every request)

**Symptoms:**
- `SocketException: Address already in use`
- `SocketException: Only one usage of each socket address is normally permitted`
- Requests start failing or hanging
- High number of connections in TIME_WAIT state

### How IHttpClientFactory Prevents Socket Exhaustion

#### 1. **Shared HttpMessageHandler**

```csharp
// Without Factory (BAD):
for (int i = 0; i < 1000; i++)
{
    using var client = new HttpClient();  // 1000 handlers = 1000 connection pools
    await client.GetAsync("https://api.example.com");
    // Each handler creates its own sockets
    // Result: 1000+ sockets, socket exhaustion!
}

// With Factory (GOOD):
for (int i = 0; i < 1000; i++)
{
    var client = factory.CreateClient("SsoService");  // Same handler reused
    await client.GetAsync("endpoint");
    // All requests share same handler and connection pool
    // Result: ~2-10 sockets (depending on concurrency), no exhaustion!
}
```

#### 2. **Connection Reuse**

- Connections are **kept alive** between requests
- Same TCP connection used for multiple HTTP requests
- Reduces socket creation/destruction

#### 3. **Proper Lifecycle Management**

- Factory manages handler lifecycle
- Handlers are disposed only when factory is disposed (application shutdown)
- No premature disposal of connection pools

#### 4. **Connection Limits**

- Default limit of 2 connections per host prevents excessive socket creation
- Can be tuned based on needs

### Socket Exhaustion Prevention in Our Implementation

**Our Current Implementation:**

```csharp
// ✅ CORRECT: Using IHttpClientFactory
public class SsoService : HttpServiceBase
{
    protected override string HttpClientName => "SsoService";
    
    // HttpClient is created per method call, but handler is shared
    public async Task<bool> ValidateTokenAsync(string token)
    {
        // CreateHttpClient() returns new wrapper, but shares handler
        HttpClient client = CreateHttpClient();
        
        // Request uses connection from shared pool
        using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
        using HttpResponseMessage response = await client.SendAsync(request);
        
        // HttpClient wrapper can be disposed (doesn't affect pool)
        // Handler and connections remain for reuse
    }
}
```

**Why This Prevents Socket Exhaustion:**

1. **One Handler per Named Client**: All requests for "SsoService" share one handler
2. **Connection Pooling**: Handler reuses connections instead of creating new ones
3. **Keep-Alive**: Connections stay open between requests
4. **No Manual Disposal**: Handler is managed by factory, not disposed prematurely

### Socket Usage Comparison

**Without IHttpClientFactory (Bad):**
```
Request 1: Create HttpClient → Create Socket 1 → Use → Close → TIME_WAIT
Request 2: Create HttpClient → Create Socket 2 → Use → Close → TIME_WAIT
Request 3: Create HttpClient → Create Socket 3 → Use → Close → TIME_WAIT
...
Request 1000: Create HttpClient → Create Socket 1000 → Use → Close → TIME_WAIT

Result: 1000 sockets in TIME_WAIT, socket exhaustion!
```

**With IHttpClientFactory (Good):**
```
Request 1: Get HttpClient → Reuse/Create Socket 1 → Use → Keep Alive
Request 2: Get HttpClient → Reuse Socket 1 → Use → Keep Alive
Request 3: Get HttpClient → Reuse Socket 1 → Use → Keep Alive
...
Request 1000: Get HttpClient → Reuse Socket 1 or 2 → Use → Keep Alive

Result: 2-10 sockets total, no exhaustion!
```

## How It Works in Our Implementation

### Current Architecture

```csharp
// 1. Registration (Startup - DependencyInjection.cs)
// Uses centralized RegisterHttpClient helper method (DRY/SOLID principles)
RegisterHttpClient(services, "SsoService", ssoServiceOptions.Url, 
    ssoServiceOptions.TimeoutSeconds, resilienceOptions, configureHeaders: null);

// This internally:
// - Validates URL (SSRF protection)
// - Sets base address and timeout
// - Configures connection pooling
// - Adds retry and circuit breaker policies with logging

// 2. Service Implementation (SsoService.cs)
public class SsoService : HttpServiceBase
{
    protected override string HttpClientName => "SsoService";
    
    // 3. Client Creation (HttpServiceBase.cs)
    protected virtual HttpClient CreateHttpClient()
    {
        return HttpClientFactory.CreateClient(HttpClientName);
        // Returns new HttpClient wrapper
        // Shares HttpMessageHandler with all other "SsoService" requests
    }
    
    // 4. Request Execution
    public async Task<bool> ValidateTokenAsync(string token)
    {
        HttpClient client = CreateHttpClient(); // Lightweight wrapper
        using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
        using HttpResponseMessage response = await client.SendAsync(request);
        // Connection from shared pool is used/reused
    }
}
```

### Lifecycle Flow

```
┌─────────────────────────────────────────────────────────────┐
│ Application Startup                                         │
│ services.AddHttpClient("SsoService", ...)                   │
│   ↓                                                          │
│ Create HttpMessageHandler Pool                              │
│   - Handler for "SsoService" (singleton, thread-safe)       │
│   - Handler for "MisService" (singleton, thread-safe)       │
│   - Handler for "AsmService" (singleton, thread-safe)       │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ Request 1: ValidateTokenAsync()                             │
│   ↓                                                          │
│ CreateHttpClient() → HttpClientFactory.CreateClient()       │
│   - Returns new HttpClient wrapper                          │
│   - Attaches to shared "SsoService" handler                 │
│   ↓                                                          │
│ SendAsync() → Uses handler's connection pool                │
│   - Check for available connection to api.uat.bapsapps.org  │
│   - No connection exists → Create new TCP connection        │
│   - Send HTTP request                                       │
│   - Keep connection alive for reuse                        │
│   ↓                                                          │
│ Dispose HttpClient wrapper (handler stays alive)           │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ Request 2: RenewTokenAsync() (concurrent with Request 1)   │
│   ↓                                                          │
│ CreateHttpClient() → HttpClientFactory.CreateClient()       │
│   - Returns new HttpClient wrapper                          │
│   - Attaches to SAME shared "SsoService" handler           │
│   ↓                                                          │
│ SendAsync() → Uses handler's connection pool                │
│   - Check for available connection                          │
│   - Connection 1 is in use → Create second connection      │
│   - Send HTTP request                                       │
│   - Keep connection alive for reuse                        │
└─────────────────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────────────────┐
│ Request 3: LogoutAsync() (after Request 1 completes)      │
│   ↓                                                          │
│ CreateHttpClient() → HttpClientFactory.CreateClient()       │
│   - Returns new HttpClient wrapper                          │
│   - Attaches to SAME shared "SsoService" handler          │
│   ↓                                                          │
│ SendAsync() → Uses handler's connection pool                 │
│   - Check for available connection                          │
│   - Connection 1 is idle → REUSE IT! (fast!)                │
│   - Send HTTP request                                       │
│   - Keep connection alive for reuse                        │
└─────────────────────────────────────────────────────────────┘
```

### Key Points

1. **One Handler per Named Client**: All "SsoService" requests share one handler
2. **Connection Pooling**: Handler maintains pool of connections per host
3. **Connection Reuse**: Idle connections are reused instead of creating new ones
4. **Thread-Safe**: Handler is thread-safe, multiple requests can use it concurrently
5. **Automatic Management**: Factory manages handler lifecycle, no manual disposal needed

## Best Practices

### ✅ DO: Use IHttpClientFactory

```csharp
// ✅ CORRECT
services.AddHttpClient("MyService", client => { /* config */ });
var client = factory.CreateClient("MyService");
```

### ❌ DON'T: Create HttpClient Directly

```csharp
// ❌ WRONG - Causes socket exhaustion
using var client = new HttpClient();
```

### ✅ DO: Create HttpClient Per Request (It's Lightweight)

```csharp
// ✅ CORRECT - HttpClient wrapper is lightweight
public async Task GetDataAsync()
{
    HttpClient client = CreateHttpClient(); // New wrapper each time is fine
    // Use client
    // Wrapper can be disposed, handler stays alive
}
```

### ❌ DON'T: Store HttpClient as Field

```csharp
// ❌ WRONG - Don't store HttpClient as instance field
public class BadService
{
    private readonly HttpClient _client; // Don't do this!
    
    public BadService(IHttpClientFactory factory)
    {
        _client = factory.CreateClient("MyService");
        // HttpClient wrapper should be created per request
    }
}
```

### ✅ DO: Use Named Clients for Different Services

```csharp
// ✅ CORRECT - Separate named clients for different services
services.AddHttpClient("SsoService", ...);
services.AddHttpClient("MisService", ...);
services.AddHttpClient("AsmService", ...);
```

### ✅ DO: Configure Service-Level Headers at Factory Level

```csharp
// ✅ CORRECT - Headers configured once at factory level
services.AddHttpClient("MisService", client =>
{
    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
    // These headers apply to all requests for this named client
});
```

### ✅ DO: Use HttpRequestMessage for Per-Request Headers

```csharp
// ✅ CORRECT - Per-request headers on HttpRequestMessage
using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
request.SetBearerToken(token); // Thread-safe per-request header
```

## Monitoring and Troubleshooting

### Check Socket Usage

**Windows:**
```powershell
netstat -an | findstr "TIME_WAIT" | measure-object -line
```

**Linux/macOS:**
```bash
netstat -an | grep TIME_WAIT | wc -l
ss -tan | grep TIME_WAIT | wc -l
```

**High TIME_WAIT count** (>1000) indicates potential socket exhaustion.

### Monitor Connection Pool

Add logging to see connection reuse:

```csharp
// In DependencyInjection.cs
services.AddHttpClient("SsoService")
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 10,
    })
    .AddHttpMessageHandler(() => new LoggingHandler()); // Custom handler to log connections
```

### Common Issues

**Issue: Socket Exhaustion Still Occurring**

**Symptoms:**
- `SocketException: Address already in use`
- High TIME_WAIT connections

**Solutions:**
1. Verify using `IHttpClientFactory` (not direct `new HttpClient()`)
2. Check for HttpClient instances stored as fields
3. Ensure HttpClient wrappers are not disposed prematurely
4. Increase `MaxConnectionsPerServer` if needed
5. Reduce connection idle timeout if too many idle connections

**Issue: Too Many Connections**

**Symptoms:**
- High number of active connections
- Resource exhaustion

**Solutions:**
1. Reduce `MaxConnectionsPerServer` (default is 2)
2. Reduce `PooledConnectionIdleTimeout` to close idle connections faster
3. Review if separate named clients are needed (each has its own pool)

**Issue: Slow Requests**

**Symptoms:**
- Requests taking longer than expected
- Connection creation overhead

**Solutions:**
1. Verify connection reuse (check logs)
2. Increase `MaxConnectionsPerServer` for high concurrency
3. Check network latency to external service
4. Review Polly retry policies (may be causing delays)

## Summary

### Key Takeaways

1. **IHttpClientFactory prevents socket exhaustion** by sharing HttpMessageHandler instances
2. **Connection pooling** reuses TCP connections across requests (much faster)
3. **One handler per named client** - all requests for same name share handler
4. **HttpClient wrapper is lightweight** - can be created/disposed per request
5. **Handler is singleton** - managed by factory, stays alive for application lifetime
6. **Thread-safe** - multiple concurrent requests can use same handler safely

### Our Implementation Benefits

✅ **No Socket Exhaustion**: Shared handlers prevent excessive socket creation  
✅ **Connection Reuse**: TCP connections are reused across requests  
✅ **Performance**: Reused connections are 20-100x faster than new connections  
✅ **Scalability**: Can handle high request volumes with minimal resources  
✅ **Thread-Safe**: Concurrent requests work correctly  
✅ **Automatic Management**: Factory handles lifecycle, no manual cleanup needed

The current implementation correctly uses `IHttpClientFactory` and follows all best practices to prevent socket exhaustion while maximizing performance through connection pooling.

