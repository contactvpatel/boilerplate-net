# Cancellation Token Guidelines

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Why Cancellation Tokens Matter](#why-cancellation-tokens-matter)
- [What Problem They Solve](#what-problem-they-solve)
- [How They Work](#how-they-work)
- [Best Practices](#best-practices)
- [Implementation Patterns](#implementation-patterns)
- [Examples from Codebase](#examples-from-codebase)
- [Common Pitfalls](#common-pitfalls)
- [When to Use Cancellation Tokens](#when-to-use-cancellation-tokens)
- [Troubleshooting](#troubleshooting)

---

## Overview

This document provides comprehensive guidelines for implementing and using `CancellationToken` in async operations throughout the WebShop API codebase. Cancellation tokens enable responsive cancellation of long-running operations, proper resource cleanup, and improved application responsiveness when clients disconnect.

## Why Cancellation Tokens Matter

### Benefits

1. **Responsive Cancellation**: Allows operations to be cancelled when clients disconnect or timeouts occur
2. **Resource Cleanup**: Ensures proper cleanup of resources when operations are cancelled
3. **Thread Safety**: Prevents thread blocking and improves application responsiveness
4. **User Experience**: Faster response times when users navigate away or cancel requests
5. **Server Efficiency**: Prevents wasted work on operations that are no longer needed
6. **Scalability**: Helps prevent resource exhaustion under high load

### Impact

- **Without Cancellation Tokens**: Operations continue even after clients disconnect, wasting server resources
- **With Cancellation Tokens**: Operations can be cancelled immediately, freeing resources for other requests
- **Proper Implementation**: Ensures all async operations respect cancellation requests

---

## What Problem They Solve

### 1. **Client Disconnection**

**Problem:** When a client disconnects (e.g., user closes browser, network timeout), the server continues processing the request, wasting CPU, memory, and database connections.

**Solution:** Cancellation tokens allow the server to detect client disconnection and stop processing immediately.

### 2. **Long-Running Operations**

**Problem:** Long-running operations (database queries, HTTP calls, file I/O) can't be interrupted, leading to resource exhaustion.

**Solution:** Cancellation tokens enable graceful cancellation of these operations.

### 3. **Timeout Management**

**Problem:** Without cancellation tokens, timeouts can't be enforced, leading to hung requests.

**Solution:** Cancellation tokens work with timeout policies to automatically cancel operations that exceed time limits.

### 4. **Resource Cleanup**

**Problem:** Cancelled operations may leave resources (database connections, HTTP connections) open.

**Solution:** Cancellation tokens enable proper cleanup in `finally` blocks or `using` statements.

### 5. **Cascading Cancellation**

**Problem:** When a parent operation is cancelled, child operations continue running.

**Solution:** Cancellation tokens can be linked to propagate cancellation through operation chains.

---

## How They Work

### Basic Mechanism

```csharp
// 1. Cancellation token is created (usually by framework)
CancellationToken cancellationToken = ...;

// 2. Token is passed to async operations
await SomeAsyncOperation(cancellationToken);

// 3. Operation checks token periodically
if (cancellationToken.IsCancellationRequested)
{
    throw new OperationCanceledException();
}

// 4. Operation throws OperationCanceledException when cancelled
```

### ASP.NET Core Integration

ASP.NET Core automatically provides cancellation tokens to controller actions:

```csharp
[HttpGet]
public async Task<ActionResult> GetData(CancellationToken cancellationToken)
{
    // cancellationToken is automatically bound from HttpContext.RequestAborted
    // It will be cancelled if the client disconnects
    return await _service.GetDataAsync(cancellationToken);
}
```

### Token Propagation

Cancellation tokens should be passed through all layers:

```
Controller → Service → Repository → Database
     ↓           ↓          ↓           ↓
  Token      Token      Token      Token
```

---

## Best Practices

### 1. **Always Accept Cancellation Tokens in Async Methods**

✅ **DO**: Accept `CancellationToken` in all async methods

```csharp
public async Task<CustomerDto?> GetByIdAsync(
    int id, 
    CancellationToken cancellationToken = default)
{
    return await _repository.GetByIdAsync(id, cancellationToken);
}
```

❌ **DON'T**: Omit cancellation tokens from async methods

```csharp
public async Task<CustomerDto?> GetByIdAsync(int id)  // Missing cancellation token
{
    return await _repository.GetByIdAsync(id);  // Can't be cancelled
}
```

### 2. **Use Default Parameter Value**

✅ **DO**: Use `= default` for optional cancellation tokens

```csharp
public async Task<T> GetAsync<T>(
    string key,
    CancellationToken cancellationToken = default)
```

❌ **DON'T**: Make cancellation token required (unless absolutely necessary)

```csharp
public async Task<T> GetAsync<T>(
    string key,
    CancellationToken cancellationToken)  // Required - less flexible
```

### 3. **Pass Tokens Through All Layers**

✅ **DO**: Pass cancellation tokens through all layers

```csharp
// Controller
public async Task<ActionResult> Get(int id, CancellationToken cancellationToken)
{
    return await _service.GetByIdAsync(id, cancellationToken);
}

// Service
public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
{
    return await _repository.GetByIdAsync(id, cancellationToken);
}

// Repository
public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken)
{
    return await connection.QueryFirstOrDefaultAsync(e => e.Id == id, cancellationToken);
}
```

❌ **DON'T**: Drop cancellation tokens between layers

```csharp
// Controller
public async Task<ActionResult> Get(int id, CancellationToken cancellationToken)
{
    return await _service.GetByIdAsync(id);  // Token not passed!
}
```

### 4. **Use RequestAborted in Filters**

✅ **DO**: Use `HttpContext.RequestAborted` in filters and middleware

```csharp
public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
{
    CancellationToken cancellationToken = context.HttpContext.RequestAborted;
    
    bool isValid = await _cacheService.GetOrCreateAsync(
        cacheKey,
        async cancel => await _ssoService.ValidateTokenAsync(token, cancel),
        cancellationToken: cancellationToken);
}
```

### 5. **Pass Tokens to Framework Methods**

✅ **DO**: Pass cancellation tokens to all framework async methods

```csharp
// Dapper ORM
await connection.QueryAsync(cancellationToken);
await connection.QueryFirstOrDefaultAsync(predicate, cancellationToken);
// Dapper executes queries immediately

// HTTP Client
await httpClient.SendAsync(request, cancellationToken);
await response.Content.ReadFromJsonAsync<T>(cancellationToken);

// Cache
await _cache.GetOrCreateAsync(key, factory, cancellationToken: cancellationToken);
```

### 6. **Handle Cancellation Gracefully**

✅ **DO**: Handle `OperationCanceledException` appropriately

```csharp
try
{
    await LongRunningOperation(cancellationToken);
}
catch (OperationCanceledException)
{
    // Cancellation is expected - log if needed, but don't treat as error
    _logger.LogInformation("Operation was cancelled");
    throw;  // Re-throw to propagate cancellation
}
```

### 7. **Don't Block on Async Operations**

✅ **DO**: Use `await` for async operations

```csharp
var result = await GetDataAsync(cancellationToken);
```

❌ **DON'T**: Use blocking calls

```csharp
var result = GetDataAsync(cancellationToken).Result;  // Blocks thread!
var result = GetDataAsync(cancellationToken).GetAwaiter().GetResult();  // Blocks thread!
GetDataAsync(cancellationToken).Wait();  // Blocks thread!
```

### 8. **Use Consistent Parameter Naming**

✅ **DO**: Use `cancellationToken` (camelCase) consistently

```csharp
public async Task<T> GetAsync<T>(
    string key,
    CancellationToken cancellationToken = default)
```

❌ **DON'T**: Use inconsistent naming

```csharp
public async Task<T> GetAsync<T>(
    string key,
    CancellationToken cancellationToken = default)  // Inconsistent
```

---

## Implementation Patterns

### Pattern 1: Controller Actions

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<Response<CustomerDto>>> GetById(
    [FromRoute] int id,
    CancellationToken cancellationToken)
{
    CustomerDto? customer = await _customerService.GetByIdAsync(id, cancellationToken);
    
    if (customer == null)
    {
        return NotFoundResponse<CustomerDto>("Customer not found", $"Customer with ID {id} not found");
    }
    
    return Ok(Response<CustomerDto>.Success(customer, "Customer retrieved successfully"));
}
```

**Key Points:**
- `CancellationToken` is automatically bound from `HttpContext.RequestAborted`
- Token is passed to service layer
- No need to create or manage the token

### Pattern 2: Service Layer

```csharp
public async Task<CustomerDto?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
{
    Customer? customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
    return customer?.Adapt<CustomerDto>();
}
```

**Key Points:**
- Accept cancellation token with `= default`
- Pass token to repository layer
- Simple pass-through pattern

### Pattern 3: Repository Layer

```csharp
public virtual async Task<T?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
{
    return await _connection.QueryAsync
        
        .QueryFirstOrDefaultAsync(e => e.Id == id, cancellationToken);
}
```

**Key Points:**
- Pass token to Dapper async methods
- All Dapper async methods accept cancellation tokens

### Pattern 4: HTTP Client Operations

```csharp
protected async Task<T?> GetAsync<T>(
    string endpoint,
    Action<HttpRequestMessage>? configureRequest,
    CancellationToken cancellationToken = default)
{
    HttpClient httpClient = CreateHttpClient();
    
    using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
    configureRequest?.Invoke(request);
    
    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
    
    // ... handle response
}
```

**Key Points:**
- Pass token to `HttpClient.SendAsync`
- Pass token to `ReadFromJsonAsync` if deserializing

### Pattern 5: Cache Operations

```csharp
bool isValid = await _cacheService.GetOrCreateAsync(
    cacheKey,
    async cancel =>
    {
        // Factory method receives its own cancellation token
        return await _ssoService.ValidateTokenAsync(token, cancel);
    },
    expiration: TimeSpan.FromMinutes(5),
    cancellationToken: cancellationToken);  // Pass outer token
```

**Key Points:**
- Factory method receives its own cancellation token
- Outer cancellation token is also passed to cache operation

### Pattern 6: Transaction Management

```csharp
public async Task<int> ExecuteAsync(string sql, object param, CancellationToken cancellationToken = default)
{
    // Dapper executes queries immediately with transaction support
    return await connection.ExecuteAsync(sql, param, transaction);
}
```

**Key Points:**
- Pass token to Dapper query methods
- All transaction operations should accept tokens

### Pattern 7: Authentication Filters

```csharp
public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
{
    // Use RequestAborted from HttpContext
    CancellationToken cancellationToken = context.HttpContext.RequestAborted;
    
    bool isValid = await _cacheService.GetOrCreateAsync(
        cacheKey,
        async cancel => await _ssoService.ValidateTokenAsync(token, cancel),
        cancellationToken: cancellationToken);
}
```

**Key Points:**
- Use `HttpContext.RequestAborted` in filters
- Pass token to all async operations

---

## Examples from Codebase

### Example 1: Complete Request Flow

**Controller:**
```csharp
[HttpGet("{id}")]
public async Task<ActionResult<Response<CustomerDto>>> GetById(
    [FromRoute] int id,
    CancellationToken cancellationToken)
{
    CustomerDto? customer = await _customerService.GetByIdAsync(id, cancellationToken);
    // ...
}
```

**Service:**
```csharp
public async Task<CustomerDto?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
{
    Customer? customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
    return customer?.Adapt<CustomerDto>();
}
```

**Repository:**
```csharp
public virtual async Task<T?> GetByIdAsync(
    int id,
    CancellationToken cancellationToken = default)
{
    return await _connection.QueryAsync
        
        .QueryFirstOrDefaultAsync(e => e.Id == id, cancellationToken);
}
```

### Example 2: HTTP Client Usage

```csharp
protected async Task<T?> GetAsync<T>(
    string endpoint,
    Action<HttpRequestMessage>? configureRequest,
    CancellationToken cancellationToken = default)
{
    HttpClient httpClient = CreateHttpClient();
    
    using HttpRequestMessage request = new(HttpMethod.Get, endpoint);
    configureRequest?.Invoke(request);
    
    using HttpResponseMessage response = await httpClient.SendAsync(request, cancellationToken);
    
    response.EnsureSuccessStatusCode();
    
    return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
}
```

### Example 3: Cache with Factory

```csharp
bool isValid = await _cacheService.GetOrCreateAsync(
    cacheKey,
    async cancel =>
    {
        _logger.LogDebug("Token not in cache, validating with SSO service");
        return await _ssoService.ValidateTokenAsync(token, cancel);
    },
    expiration: cacheExpiration,
    cancellationToken: context.HttpContext.RequestAborted);
```

### Example 4: Multiple Database Operations

```csharp
public async Task<CustomerDto> CreateAsync(
    CreateCustomerDto createDto,
    CancellationToken cancellationToken = default)
{
    Customer customer = createDto.Adapt<Customer>();
    await _customerRepository.AddAsync(customer, cancellationToken);
    // Dapper executes immediately; no separate SaveChanges needed
    return customer.Adapt<CustomerDto>();
}
```

---

## Common Pitfalls

### Pitfall 1: Forgetting to Pass Token

❌ **Wrong:**
```csharp
public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
{
    // Token not passed to repository!
    return await _repository.GetByIdAsync(id);
}
```

✅ **Correct:**
```csharp
public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken)
{
    return await _repository.GetByIdAsync(id, cancellationToken);
}
```

### Pitfall 2: Not Using Token in Framework Methods

❌ **Wrong:**
```csharp
var customers = await connection.QueryAsync();  // Missing token
```

✅ **Correct:**
```csharp
var customers = await connection.QueryAsync(cancellationToken);
```

### Pitfall 3: Blocking Async Operations

❌ **Wrong:**
```csharp
var result = GetDataAsync(cancellationToken).Result;  // Blocks thread!
```

✅ **Correct:**
```csharp
var result = await GetDataAsync(cancellationToken);
```

### Pitfall 4: Creating New Tokens Unnecessarily

❌ **Wrong:**
```csharp
public async Task GetData(CancellationToken cancellationToken)
{
    var cts = new CancellationTokenSource();  // Unnecessary!
    return await _service.GetDataAsync(cts.Token);
}
```

✅ **Correct:**
```csharp
public async Task GetData(CancellationToken cancellationToken)
{
    return await _service.GetDataAsync(cancellationToken);  // Use provided token
}
```

### Pitfall 5: Not Handling Cancellation

❌ **Wrong:**
```csharp
try
{
    await LongOperation(cancellationToken);
}
catch (Exception ex)
{
    // OperationCanceledException treated as error
    _logger.LogError(ex, "Operation failed");
}
```

✅ **Correct:**
```csharp
try
{
    await LongOperation(cancellationToken);
}
catch (OperationCanceledException)
{
    // Cancellation is expected - don't treat as error
    _logger.LogInformation("Operation was cancelled");
    throw;  // Re-throw to propagate
}
catch (Exception ex)
{
    _logger.LogError(ex, "Operation failed");
}
```

### Pitfall 6: Using .Count() on IQueryable

❌ **Wrong:**
```csharp
var count = await _connection.QueryAsync.Where(x => x.IsActive).Count();  // Synchronous, blocks!
```

✅ **Correct:**
```csharp
var count = await _connection.QueryAsync.Where(x => x.IsActive).QuerySingleAsync<int>(sql, cancellationToken);
```

---

## When to Use Cancellation Tokens

### ✅ Always Use Cancellation Tokens For:

1. **All async methods** - Every async method should accept a cancellation token
2. **Database operations** - All Dapper async methods
3. **HTTP calls** - All HttpClient operations
4. **File I/O** - All file read/write operations
5. **Cache operations** - All cache get/set/remove operations
6. **Long-running operations** - Any operation that might take more than a few milliseconds

### ⚠️ Optional (But Recommended) For:

1. **Very fast operations** - Operations that complete in < 1ms (still recommended for consistency)
2. **Synchronous operations** - Not applicable (cancellation tokens are for async operations)

### ❌ Don't Use Cancellation Tokens For:

1. **Synchronous methods** - Cancellation tokens are for async operations only
2. **CPU-bound operations** - Use `Task.Run` with cancellation token if needed
3. **Fire-and-forget operations** - Operations that shouldn't be cancelled

---

## Troubleshooting

### Issue: Operations Not Cancelling

**Symptoms:**
- Operations continue even after client disconnects
- Timeouts not working
- Resources not being freed

**Solutions:**

1. **Check Token Propagation**
   ```csharp
   // Verify token is passed through all layers
   Controller → Service → Repository → Database
   ```

2. **Verify Framework Methods**
   ```csharp
   // Ensure all framework methods receive token
   await connection.QueryAsync(cancellationToken);  // ✅
   await httpClient.SendAsync(request, cancellationToken);  // ✅
   ```

3. **Check Token Source**
   ```csharp
   // In controllers, token comes from HttpContext.RequestAborted
   public async Task Get(CancellationToken cancellationToken)  // ✅ Auto-bound
   ```

### Issue: OperationCanceledException in Logs

**Symptoms:**
- `OperationCanceledException` appearing in error logs
- Operations being treated as errors when cancelled

**Solutions:**

1. **Handle Cancellation Separately**
   ```csharp
   try
   {
       await Operation(cancellationToken);
   }
   catch (OperationCanceledException)
   {
       // Expected - don't log as error
       throw;  // Re-throw to propagate
   }
   catch (Exception ex)
   {
       // Actual errors
       _logger.LogError(ex, "Operation failed");
   }
   ```

2. **Use LogLevel.Information for Cancellation**
   ```csharp
   catch (OperationCanceledException)
   {
       _logger.LogInformation("Operation was cancelled");
       throw;
   }
   ```

### Issue: Token Not Available in Filters

**Symptoms:**
- Need cancellation token in filters or middleware
- Token not available in constructor

**Solutions:**

1. **Use HttpContext.RequestAborted**
   ```csharp
   public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
   {
       CancellationToken cancellationToken = context.HttpContext.RequestAborted;
       // Use token...
   }
   ```

2. **Access in Middleware**
   ```csharp
   public async Task InvokeAsync(HttpContext context, RequestDelegate next)
   {
       CancellationToken cancellationToken = context.RequestAborted;
       // Use token...
   }
   ```

### Issue: Multiple Cancellation Tokens

**Symptoms:**
- Need to combine multiple cancellation tokens
- Need timeout in addition to client cancellation

**Solutions:**

1. **Use CancellationTokenSource.CreateLinkedTokenSource**
   ```csharp
   using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
   using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
       cancellationToken,
       timeoutCts.Token);
   
   await Operation(linkedCts.Token);
   ```

2. **Use CancellationTokenSource with Timeout**
   ```csharp
   using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
   await Operation(cts.Token);
   ```

### Issue: Token Not Working in Parallel Operations

**Symptoms:**
- Cancellation not working with `Task.WhenAll`
- Some operations continue after cancellation

**Solutions:**

1. **Pass Token to All Tasks**
   ```csharp
   var tasks = new[]
   {
       Operation1(cancellationToken),
       Operation2(cancellationToken),
       Operation3(cancellationToken)
   };
   
   await Task.WhenAll(tasks);
   ```

2. **Use CancellationToken.ThrowIfCancellationRequested**
   ```csharp
   public async Task ProcessItemsAsync(
       IEnumerable<Item> items,
       CancellationToken cancellationToken)
   {
       foreach (var item in items)
       {
           cancellationToken.ThrowIfCancellationRequested();
           await ProcessItemAsync(item, cancellationToken);
       }
   }
   ```

---

## Summary

Cancellation tokens are essential for building responsive, scalable applications. Key takeaways:

✅ **Always accept** `CancellationToken` in async methods  
✅ **Always pass** tokens through all layers  
✅ **Always use** tokens with framework async methods  
✅ **Always handle** `OperationCanceledException` appropriately  
✅ **Never block** on async operations  
✅ **Never drop** tokens between layers  

Following these guidelines ensures your application can gracefully handle cancellation, improve responsiveness, and efficiently manage resources.

---

## References

- [Cancellation Tokens in .NET](https://learn.microsoft.com/en-us/dotnet/standard/threading/cancellation-in-managed-threads)
- [ASP.NET Core Cancellation Tokens](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/http-requests#cancellation-tokens)
- [Dapper Cancellation Tokens](https://learn.microsoft.com/en-us/ef/core/miscellaneous/async)
- [HttpClient Cancellation Tokens](https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.sendasync)

