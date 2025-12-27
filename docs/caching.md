# Caching Implementation Guide

## Overview

The WebShop API uses **HybridCache** (Microsoft.Extensions.Caching.Hybrid) for intelligent, multi-tier caching. HybridCache combines in-memory caching with optional distributed caching, providing optimal performance, stampede protection, and scalability for modern .NET applications.

## Table of Contents

- [Why HybridCache?](#why-hybridcache)
- [What Problem It Solves](#what-problem-it-solves)
- [How It Works](#how-it-works)
- [Architecture & Design](#architecture--design)
- [Benefits](#benefits)
- [Implementation Details](#implementation-details)
- [Configuration](#configuration)
- [Usage Examples](#usage-examples)
- [Cache Management](#cache-management)
- [Best Practices](#best-practices)
- [Troubleshooting](#troubleshooting)

## Why HybridCache?

### The Problem with Traditional Caching

Traditional caching solutions like `IMemoryCache` have limitations:

1. **Single-Tier Only**: Only in-memory, no distributed cache support
2. **No Stampede Protection**: Multiple concurrent requests can cause cache stampede (thundering herd)
3. **Manual Coordination**: Developers must manually handle cache invalidation and coordination
4. **No Automatic Fallback**: If cache miss, all requests hit the data source simultaneously
5. **Limited Scalability**: In-memory cache doesn't scale across multiple servers

### The Solution: HybridCache

HybridCache provides:

- **Two-Tier Architecture**: In-memory (primary) + distributed (secondary) cache
- **Automatic Stampede Protection**: Only one request executes the factory, others wait
- **Intelligent Caching**: Automatic coordination between tiers
- **Performance Optimized**: Fast in-memory access with distributed cache backup
- **Scalable**: Works across multiple servers with distributed cache

## What Problem It Solves

### 1. **Cache Stampede (Thundering Herd)**

**Problem:** When cache expires, multiple concurrent requests all try to refresh it simultaneously, overwhelming the data source.

**Solution:** HybridCache ensures only one request executes the factory method. Other requests wait and receive the cached result.

### 2. **Performance**

**Problem:** Database queries are slow, especially for frequently accessed data.

**Solution:** HybridCache provides fast in-memory access (nanoseconds) with distributed cache backup for multi-server scenarios.

### 3. **Scalability**

**Problem:** In-memory cache doesn't work across multiple servers.

**Solution:** HybridCache can use distributed cache (Redis, SQL Server, PostgreSQL) as secondary storage, enabling cache sharing across servers.

### 4. **Consistency**

**Problem:** Manual cache management leads to inconsistencies and bugs.

**Solution:** HybridCache provides a clean abstraction (`ICacheService`) with consistent behavior.

## How It Works

### Two-Tier Architecture

```
Request for cached data
    ↓
1. Check in-memory cache (primary)
    ↓
1a. If found: Return immediately (fastest)
    ↓
1b. If not found: Check distributed cache (secondary)
    ↓
2a. If found in distributed:
    - Store in in-memory cache
    - Return to caller
    ↓
2b. If not found:
    - Execute factory method (only one request)
    - Store in both caches
    - Return to caller
    - Other waiting requests receive cached result
```

### Stampede Protection

When multiple requests arrive simultaneously for the same cache key:

```
Request 1: Cache miss → Executes factory
Request 2: Cache miss → Waits for Request 1
Request 3: Cache miss → Waits for Request 1
    ↓
Request 1: Completes → Stores in cache → Returns result
    ↓
Request 2: Receives cached result (no factory execution)
Request 3: Receives cached result (no factory execution)
```

**Benefit:** Only one database query instead of three.

### Cache Expiration

HybridCache supports two expiration times:

- **Expiration**: Total cache lifetime (in-memory + distributed)
- **LocalExpiration**: In-memory cache lifetime (shorter, for faster refresh)

**Example:**

- `Expiration: 10 minutes` - Data stays in cache for 10 minutes total
- `LocalExpiration: 5 minutes` - In-memory cache refreshes after 5 minutes, but distributed cache still has it

## Architecture & Design

### Clean Architecture

The caching implementation follows Clean Architecture:

```
WebShop.Core (Interfaces)
    ↓
ICacheService (abstraction)
    ↓
WebShop.Infrastructure (Implementation)
    ↓
CacheService (HybridCache wrapper)
    ↓
WebShop.Business (Usage)
    ↓
Services use ICacheService
```

**Benefits:**

- **Testability**: Can mock `ICacheService` in tests
- **Flexibility**: Can swap implementations without changing business logic
- **Separation of Concerns**: Business layer doesn't know about HybridCache

### Service Registration

In `DependencyInjection.cs`:

```csharp
// Register CacheOptions
services.Configure<CacheOptions>(configuration.GetSection("CacheOptions"));

// Register HybridCache only if caching is enabled
CacheOptions cacheOptions = new();
configuration.GetSection("CacheOptions").Bind(cacheOptions);

if (cacheOptions.Enabled)
{
    services.AddHybridCache(options => { /* configuration */ });
    
    // Register distributed cache (optional)
    // services.AddStackExchangeRedisCache(options => { /* Redis config */ });
}

// Register ICacheService (handles disabled state internally)
services.AddScoped<ICacheService, CacheService>();
```

**Key Points:**

- HybridCache is only registered when `Enabled: true`
- `CacheService` checks the `Enabled` flag internally
- When disabled, `CacheService` executes factory methods directly (bypasses cache)
- No separate no-op service needed

## Benefits

### 1. **Performance**

- **In-memory access**: Nanosecond latency
- **Distributed cache**: Millisecond latency (vs. database query: 10-100ms)
- **Reduced database load**: Fewer queries = better performance

### 2. **Scalability**

- **Multi-server support**: Distributed cache enables cache sharing
- **Load distribution**: Cache reduces load on all servers
- **Horizontal scaling**: Add more servers without cache coordination issues

### 3. **Reliability**

- **Stampede protection**: Prevents overwhelming data sources
- **Automatic fallback**: If distributed cache fails, in-memory still works
- **Error handling**: Graceful degradation on cache errors

### 4. **Developer Experience**

- **Simple API**: `GetOrCreateAsync()` handles everything
- **No manual coordination**: HybridCache handles synchronization
- **Clean abstraction**: `ICacheService` hides complexity

## Implementation Details

### ICacheService Interface

```csharp
public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        CancellationToken cancellationToken = default) where T : notnull;

    Task SetAsync<T>(string key, T value, ...);
    Task RemoveAsync(string key, ...);
    Task RemoveByTagAsync(string tag, ...);
}
```

### GetOrCreateAsync Pattern

The most common pattern:

```csharp
var result = await _cacheService.GetOrCreateAsync(
    cacheKey,
    async cancel => await _repository.GetDataAsync(cancel),
    expiration: TimeSpan.FromMinutes(10),
    localExpiration: TimeSpan.FromMinutes(5),
    cancellationToken: cancellationToken);
```

**What happens (when caching is enabled):**

1. Check in-memory cache
2. If miss, check distributed cache
3. If miss, execute factory (only one request)
4. Store in both caches
5. Return result

**What happens (when caching is disabled):**

1. Execute factory directly (service/database call)
2. Return result (no caching)

### Cache Key Naming

**Best Practices:**

- Use consistent prefixes: `"product-{id}"`, `"customer-{id}"`
- Include entity type: `"product"`, `"customer"`
- Include identifier: `"{id}"`, `"{email}"`
- Avoid user input directly in keys (sanitize first)

**Examples:**

```csharp
$"product-{productId}"
$"customer-email-{email.ToLowerInvariant()}"
$"order-{orderId}-positions"
```

### Tag-Based Cache Management

Tags allow grouping related cache entries:

```csharp
// Cache with tag
await _cacheService.GetOrCreateAsync(
    $"product-{id}",
    factory,
    tags: new[] { "products", $"product-{id}" });

// Clear all products
await _cacheService.RemoveByTagAsync("products");
```

**Use Cases:**

- Clear all related cache entries when entity is updated
- Invalidate cache by category
- Clear user-specific cache on logout

### CacheService Implementation

The `CacheService` class wraps HybridCache and provides the `ICacheService` interface. It handles both enabled and disabled states:

```csharp
public class CacheService(
    HybridCache? cache,
    IOptions<CacheOptions> cacheOptions,
    ILogger<CacheService> logger) : ICacheService
{
    private readonly HybridCache? _cache = cache;
    private readonly CacheOptions _cacheOptions = cacheOptions?.Value ?? new CacheOptions();
    
    public async Task<T> GetOrCreateAsync<T>(...)
    {
        // If caching is disabled, execute factory directly
        if (!_cacheOptions.Enabled || _cache == null)
        {
            return await factory(cancellationToken);
        }
        
        // Otherwise, use HybridCache
        return await _cache.GetOrCreateAsync(...);
    }
}
```

**Key Features:**

- **Enabled State**: When `Enabled: true`, uses HybridCache for caching
- **Disabled State**: When `Enabled: false`, executes factory methods directly (service/database calls)
- **No Separate Service**: Single service handles both states (no no-op service needed)
- **Graceful Degradation**: If HybridCache is unavailable, falls back to direct execution

## Configuration

### appsettings.json

```json
{
  "CacheOptions": {
    "Enabled": true,
    "DefaultExpiration": "00:10:00",
    "DefaultLocalExpiration": "00:05:00",
    "MaximumPayloadBytes": 1048576,
    "MaximumKeyLength": 1024,
    "RedisConnectionString": null,
    "RedisInstanceName": null
  }
}
```

### Configuration Options

- **`Enabled`** (boolean, default: `true`): Whether caching is enabled. When `false`, all cache operations bypass the cache and execute factory methods directly (service/database calls).
- **`DefaultExpiration`**: Default cache lifetime (TimeSpan format: `"HH:mm:ss"`)
- **`DefaultLocalExpiration`**: Default in-memory cache lifetime
- **`MaximumPayloadBytes`**: Maximum size of cached values (default: 1MB)
- **`MaximumKeyLength`**: Maximum cache key length (default: 1024)
- **`RedisConnectionString`**: Connection string for redis distributed cache
- **`RedisInstanceName`** (string, optional): Redis instance name for key prefixing (e.g., `"webshop-dev:"`). Useful when multiple applications share the same Redis instance. Uses colon (`:`) separator as industry standard. If not specified, no prefix is used.

### Disabling Caching

To disable caching entirely, set `Enabled` to `false`:

```json
{
  "CacheOptions": {
    "Enabled": false
  }
}
```

**Behavior when disabled:**

- `GetOrCreateAsync` executes the factory method directly (service/database call)
- `SetAsync`, `RemoveAsync`, and `RemoveByTagAsync` are no-ops
- HybridCache is not registered (no memory overhead)
- All cache operations bypass the cache layer

**Use cases for disabling:**

- Troubleshooting cache-related issues
- Testing without cache interference
- Development scenarios where cache is not needed
- Temporary disabling during cache migration

### Distributed Cache Setup

#### Redis (Recommended for Production)

Redis distributed cache is now fully implemented and ready to use.

1. **Package**: Already added to `Directory.Packages.props`:

```xml
<PackageVersion Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.1" />
```

1. **Update `appsettings.json`**:

```json
{
  "CacheOptions": {
    "Enabled": true,
    "RedisConnectionString": "localhost:6379",
    "RedisInstanceName": "webshop-dev:"
  }
}
```

**Connection String Formats:**

- Simple: `"localhost:6379"`
- With password: `"localhost:6379,password=yourpassword"`
- Full connection: `"server:port,password=pass,ssl=true,abortConnect=false"`
- Cloud Redis Cache: `"your-cache.redis.cache.windows.net:6380,password=key,ssl=True,abortConnect=False"`

**Redis Configuration Options:**

- **`RedisInstanceName`**:
  - **Purpose**: Prefixes all cache keys with the instance name (e.g., `webshop-dev:product:123`)
  - **Use Case**: When multiple applications share the same Redis instance
  - **Format**: Use colon (`:`) separator as industry standard (e.g., `"webshop-dev:"`, `"webshop-prod:"`)
  - **Best Practice**: Include environment name (dev, staging, prod) for key isolation
  - **Example**: `"webshop-dev:"` creates keys like `webshop-dev:user:1000`

1. **Implementation**: Redis is automatically registered in `DependencyInjection.cs` when `RedisConnectionString` has value. The implementation uses `ConfigurationOptions.Parse()` for advanced configuration control and includes certificate validation support. HybridCache will automatically use the registered Redis cache as the distributed cache layer.

**Advanced Features:**

- **Instance Name Prefixing**: All cache keys are automatically prefixed with `RedisInstanceName` for multi-tenant scenarios
- **Certificate Validation**: Custom validation allows `RemoteCertificateNameMismatch` (common with cloud Redis services) when validation is disabled
- **Connection Control**: Uses `ConfigurationOptions.Parse()` for fine-grained Redis connection settings

**Benefits:**

- Cache shared across multiple servers
- High performance and scalability
- Automatic failover support
- Production-ready solution
- Key isolation through instance naming
- Flexible certificate validation for cloud deployments

#### SQL Server

Similar process with `Microsoft.Extensions.Caching.SqlServer` package.

#### PostgreSQL

Similar process with `Microsoft.Extensions.Caching.Postgres` package.

## Usage Examples

### Example 1: Basic Caching

```csharp
public async Task<ProductDto> GetProductAsync(int id, CancellationToken cancellationToken)
{
    string cacheKey = $"product-{id}";
    
    return await _cacheService.GetOrCreateAsync(
        cacheKey,
        async cancel => await _productRepository.GetByIdAsync(id, cancel),
        expiration: TimeSpan.FromMinutes(10),
        cancellationToken: cancellationToken);
}
```

### Example 2: Caching with Tags

```csharp
public async Task<ProductDto> GetProductAsync(int id, CancellationToken cancellationToken)
{
    string cacheKey = $"product-{id}";
    
    return await _cacheService.GetOrCreateAsync(
        cacheKey,
        async cancel => await _productRepository.GetByIdAsync(id, cancel),
        expiration: TimeSpan.FromMinutes(10),
        tags: new[] { "products", $"product-{id}" },
        cancellationToken: cancellationToken);
}

// When product is updated, clear cache
public async Task UpdateProductAsync(int id, UpdateProductDto dto)
{
    await _productRepository.UpdateAsync(id, dto);
    
    // Clear specific product cache
    await _cacheService.RemoveAsync($"product-{id}");
    
    // Or clear all products
    await _cacheService.RemoveByTagAsync("products");
}
```

### Example 3: SSO Token Validation Caching

```csharp
public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken)
{
    string tokenHash = GetTokenHash(token);
    string cacheKey = $"token-validation-{tokenHash}";
    
    bool isValid = await _cacheService.GetOrCreateAsync(
        cacheKey,
        async cancel => await _ssoService.ValidateTokenAsync(token, cancel),
        expiration: TimeSpan.FromMinutes(5), // Cache valid tokens for 5 minutes
        localExpiration: TimeSpan.FromMinutes(5),
        cancellationToken: cancellationToken);
    
    // Cache invalid tokens for shorter duration
    if (!isValid)
    {
        await _cacheService.SetAsync(
            cacheKey,
            false,
            expiration: TimeSpan.FromSeconds(30),
            cancellationToken: cancellationToken);
    }
    
    return isValid;
}
```

### Example 4: Conditional Caching

```csharp
public async Task<List<ProductDto>> GetProductsAsync(bool includeInactive, CancellationToken cancellationToken)
{
    string cacheKey = $"products-{(includeInactive ? "all" : "active")}";
    
    return await _cacheService.GetOrCreateAsync(
        cacheKey,
        async cancel => await _productRepository.GetAllAsync(includeInactive, cancel),
        expiration: TimeSpan.FromMinutes(5),
        cancellationToken: cancellationToken);
}
```

## Cache Management

### Cache Management Controller

The API provides a `CacheManagementController` for administrative cache operations:

#### Clear by Key

```http
DELETE /api/v1/cachemanagement/key/{key}
```

#### Clear by Multiple Keys

```http
DELETE /api/v1/cachemanagement/keys
Content-Type: application/json

{
  "keys": ["product-1", "product-2", "customer-123"]
}
```

#### Clear by Tag

```http
DELETE /api/v1/cachemanagement/tag
Content-Type: application/json

{
  "tag": "products"
}
```

#### Clear by Multiple Tags

```http
DELETE /api/v1/cachemanagement/tags
Content-Type: application/json

{
  "tags": ["products", "customers"]
}
```

### When to Clear Cache

1. **Entity Updates**: Clear specific entity cache
2. **Bulk Updates**: Clear tag-based cache
3. **Data Migration**: Clear all cache
4. **Configuration Changes**: Clear related cache
5. **Scheduled Maintenance**: Clear stale cache

## Best Practices

### 1. **Use GetOrCreateAsync for Read Operations**

```csharp
// Good: Automatic cache management
var product = await _cacheService.GetOrCreateAsync(
    $"product-{id}",
    async cancel => await _repository.GetByIdAsync(id, cancel));

// Avoid: Manual cache management
var product = await _cacheService.GetAsync<ProductDto>($"product-{id}");
if (product == null)
{
    product = await _repository.GetByIdAsync(id);
    await _cacheService.SetAsync($"product-{id}", product);
}
```

### 2. **Choose Appropriate Expiration Times**

- **Frequently changing data**: 1-5 minutes
- **Moderately changing data**: 10-30 minutes
- **Rarely changing data**: 1-24 hours
- **Static data**: 24+ hours

### 3. **Use Tags for Related Data**

```csharp
// Cache with tags
await _cacheService.GetOrCreateAsync(
    $"product-{id}",
    factory,
    tags: new[] { "products", $"product-{id}" });

// Clear all related cache
await _cacheService.RemoveByTagAsync("products");
```

### 4. **Handle Cache Errors Gracefully**

```csharp
try
{
    return await _cacheService.GetOrCreateAsync(key, factory);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Cache error for key: {Key}", key);
    // Fallback to direct data access
    return await factory(cancellationToken);
}
```

### 5. **Don't Cache Sensitive Data**

- Never cache passwords, tokens (except validation results), or PII
- Be careful with user-specific data
- Consider cache encryption for sensitive cached data

### 6. **Monitor Cache Performance**

- Track cache hit rates
- Monitor cache size
- Watch for cache stampede patterns
- Review expiration times

### 7. **Use Consistent Key Naming**

```csharp
// Good: Consistent pattern
$"product-{id}"
$"customer-{id}"
$"order-{id}"

// Bad: Inconsistent
$"Product_{id}"
$"customer{id}"
$"order:{id}"
```

## Troubleshooting

### Issue: Cache Not Working

**Symptoms:** Data always fetched from source, never from cache

**Solutions:**

1. Check cache key is consistent
2. Verify expiration time is not too short
3. Check cache service is registered
4. Review logs for cache errors
5. Verify factory method is being called (should only be called once per key)

### Issue: Stale Data

**Symptoms:** Cached data is outdated

**Solutions:**

1. Clear cache when data is updated
2. Reduce expiration time
3. Use tags for easier cache invalidation
4. Check cache invalidation logic

### Issue: High Memory Usage

**Symptoms:** Application using too much memory

**Solutions:**

1. Reduce `MaximumPayloadBytes`
2. Reduce expiration times
3. Clear unused cache entries
4. Monitor cache size
5. Consider distributed cache to offload memory

### Issue: Cache Stampede Still Happening

**Symptoms:** Multiple requests hitting data source simultaneously

**Solutions:**

1. Verify HybridCache is being used (not IMemoryCache)
2. Check `GetOrCreateAsync` is used (not `GetAsync` + `SetAsync`)
3. Review factory method execution logs
4. Ensure cache service is properly registered
5. Verify `CacheOptions.Enabled` is set to `true`

### Issue: Cache Not Working (Always Hitting Database)

**Symptoms:** Cache operations always execute factory methods, never using cache

**Solutions:**

1. Check `CacheOptions.Enabled` is set to `true` in `appsettings.json`
2. Verify HybridCache is registered in `DependencyInjection.cs`
3. Check application logs for "Cache disabled" messages
4. Ensure `CacheOptions` configuration section is properly bound

### Issue: Distributed Cache Not Working

**Symptoms:** Cache not shared across servers

**Solutions:**

1. Verify distributed cache is configured
2. Check connection string is correct
3. Verify distributed cache service is running
4. Check network connectivity
5. Review distributed cache logs

## Related Documentation

- [HybridCache Documentation](https://learn.microsoft.com/en-us/aspnet/core/performance/caching/hybrid?view=aspnetcore-10.0)
- [ICacheService Interface](../src/WebShop.Core/Interfaces/ICacheService.cs)
- [CacheService Implementation](../src/WebShop.Infrastructure/Services/CacheService.cs)
- [Cache Management Controller](../src/WebShop.Api/Controllers/CacheManagementController.cs)

## Summary

HybridCache provides:

- ✅ Two-tier caching (in-memory + distributed)
- ✅ Automatic stampede protection
- ✅ Optimal performance
- ✅ Scalability across multiple servers
- ✅ Clean abstraction via `ICacheService`
- ✅ Tag-based cache management
- ✅ Configurable expiration times

By using HybridCache, developers can improve application performance, reduce database load, and scale horizontally while maintaining clean, testable code through the `ICacheService` abstraction.
