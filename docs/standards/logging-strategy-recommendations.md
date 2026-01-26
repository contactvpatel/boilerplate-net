# Logging Guidelines for Clean Architecture

[← Back to README](../../README.md)

## Purpose

This document defines **when and what to log** at each layer in a Clean Architecture application. These guidelines ensure consistent, non-redundant logging that provides optimal observability while maintaining performance and separation of concerns.

---

## Core Principles

1. **Log at the layer where the concern belongs** - Each layer logs only what it's responsible for
2. **Avoid duplication** - Don't log the same information at multiple layers
3. **Log contextually** - Include relevant identifiers (EntityId, RequestId, UserId) for correlation
4. **Performance matters** - Minimize logging in hot paths; use appropriate log levels
5. **Leverage framework capabilities** - Use OpenTelemetry for automatic query instrumentation

---

## Layer-Specific Logging Rules

### 1. Controller Layer (API/Presentation)

**Responsibility**: HTTP request/response handling, API-level concerns

**Important**: OpenTelemetry already captures HTTP context (method, path, route params, query params, status codes, duration). Business Service already logs business events (Create/Update/Delete). Controller logging should be **minimal** to avoid triple redundancy.

#### ✅ MUST Log

1. **HTTP-Level Error Outcomes Only**
   - Entity not found scenarios (404 Not Found)
   - Include entity ID for correlation

#### ❌ MUST NOT Log

1. **HTTP Technical Details** (OpenTelemetry handles this)
   - HTTP method and path
   - Route parameters
   - Query parameters
   - Status codes (except 404 for troubleshooting)

2. **Business Operations** (Business Service handles this)
   - "Creating address" - Business Service already logs
   - "Updating address" - Business Service already logs
   - "Deleting address" - Business Service already logs
   - Business-relevant data (CustomerId, City, etc.)

3. **Success Messages**
   - "Retrieving all addresses" - OpenTelemetry shows GET request
   - "Address retrieved successfully" - Status 200 indicates success

#### Example: GET Operations (No Logging)

```csharp
[HttpGet]
public async Task<ActionResult<Response<IReadOnlyList<AddressDto>>>> GetAll(CancellationToken cancellationToken)
{
    // No logging - OpenTelemetry captures GET /api/v1/addresses
    IReadOnlyList<AddressDto> addresses = await _addressService.GetAllAsync(cancellationToken);
    return Ok(Response<IReadOnlyList<AddressDto>>.Success(addresses, "Addresses retrieved successfully"));
}

[HttpGet("{id}")]
public async Task<ActionResult<Response<AddressDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
{
    // No logging - OpenTelemetry captures GET /api/v1/addresses/{id} with route.id
    AddressDto? address = await _addressService.GetByIdAsync(id, cancellationToken);
    if (address == null)
    {
        // ✅ Log only HTTP-level error outcome (404 Not Found)
        _logger.LogWarning("Address not found. AddressId: {AddressId}", id);
        return HandleNotFound<AddressDto>("Address", "ID", id) 
            ?? NotFoundResponse<AddressDto>("Address not found", $"Address with ID {id} not found.");
    }
    return Ok(Response<AddressDto>.Success(address, "Address retrieved successfully"));
}
```

#### Example: Create/Update/Delete Operations (No Controller Logging)

```csharp
[HttpPost]
public async Task<ActionResult<Response<AddressDto>>> Create([FromBody] CreateAddressDto createDto, CancellationToken cancellationToken)
{
    // No logging - Business Service logs "Creating address" and "Address created"
    // OpenTelemetry captures POST /api/v1/addresses with status 201
    AddressDto address = await _addressService.CreateAsync(createDto, cancellationToken);
    Response<AddressDto> response = Response<AddressDto>.Success(address, "Address created successfully");
    return CreatedAtAction(nameof(GetById), new { id = address.Id }, response);
}

[HttpPut("{id}")]
public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateAddressDto updateDto, CancellationToken cancellationToken)
{
    // No logging - Business Service logs "Updating address" and "Address updated"
    // OpenTelemetry captures PUT /api/v1/addresses/{id} with status 204
    AddressDto? address = await _addressService.UpdateAsync(id, updateDto, cancellationToken);
    if (address == null)
    {
        // ✅ Log only HTTP-level error outcome (404 Not Found)
        _logger.LogWarning("Address not found for update. AddressId: {AddressId}", id);
        return HandleNotFound<AddressDto>("Address", "ID", id) 
            ?? NotFoundResponse<AddressDto>("Address not found", $"Address with ID {id} not found.");
    }
    return NoContent();
}
```

#### Log Level Guidelines

- `LogWarning`: HTTP-level error outcomes (404 Not Found)
- `LogError`: Exceptions (handled by ExceptionHandlingMiddleware)
- `LogInformation`: Not used in Controllers (OpenTelemetry and Business Service handle this)

---

### 2. Service Layer (Business Logic)

**Responsibility**: Business rules, domain logic, orchestrations

#### ✅ MUST Log

1. **Complex Business Logic Operations**
   - Multi-step workflows
   - Cross-entity operations
   - Business rule validations
   - Domain events

2. **Business Rule Violations**
   - Validation failures
   - Constraint violations
   - Business exceptions

3. **Critical Business Events**
   - Entity creation (with business context)
   - Entity updates (with business context)
   - Entity deletion (with business context)
   - State transitions

4. **Service-Level Errors**
   - Business logic exceptions
   - Orchestration failures

#### ❌ MUST NOT Log

- Simple CRUD pass-through operations
- Direct repository calls without business logic
- Information already logged at Controller layer
- HTTP concerns

#### When to Log

| Operation Type | Log? | Reason |
|---------------|------|--------|
| Simple GetById/GetAll | ❌ No | Controller already logs HTTP request |
| Simple Create/Update/Delete | ✅ Yes | Business event (entity lifecycle) |
| Complex business operations | ✅ Yes | Business logic with validations |
| Multi-step workflows | ✅ Yes | Orchestration across entities |
| Business rule validations | ✅ Yes | Domain logic decisions |

#### Example: Simple CRUD (No Logging)

```csharp
// ❌ NO LOGGING - Simple pass-through
public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
{
    Customer? customer = await _customerRepository.GetByIdAsync(id, cancellationToken);
    return customer?.Adapt<CustomerDto>();
}
```

#### Example: Business Event (Logging Required)

```csharp
// ✅ MUST LOG - Business event
public async Task<CustomerDto> CreateAsync(CreateCustomerDto createDto, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Creating customer. FirstName: {FirstName}, LastName: {LastName}, Email: {Email}", 
        createDto.FirstName, createDto.LastName, createDto.Email);
    
    Customer customer = createDto.Adapt<Customer>();
    await _customerRepository.AddAsync(customer, cancellationToken);
    await // Dapper commits immediately(cancellationToken);
    
    _logger.LogInformation("Customer created successfully. CustomerId: {CustomerId}", customer.Id);
    return customer.Adapt<CustomerDto>();
}
```

#### Example: Complex Business Logic (Logging Required)

```csharp
// ✅ MUST LOG - Complex business logic
public async Task<OrderDto> CreateAsync(CreateOrderDto createDto, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Creating order with business validation. CustomerId: {CustomerId}, ItemsCount: {ItemsCount}", 
        createDto.CustomerId, createDto.Items?.Count ?? 0);
    
    // Business rule: Validate customer
    Customer? customer = await _customerRepository.GetByIdAsync(createDto.CustomerId, cancellationToken);
    if (customer == null)
    {
        _logger.LogWarning("Order creation failed: Customer not found. CustomerId: {CustomerId}", createDto.CustomerId);
        throw new BusinessException("Customer not found");
    }
    
    // Business rule: Validate inventory
    foreach (var item in createDto.Items)
    {
        Stock? stock = await _stockRepository.GetByArticleIdAsync(item.ArticleId, cancellationToken);
        if (stock == null || stock.Quantity < item.Quantity)
        {
            _logger.LogWarning("Order creation failed: Insufficient stock. ArticleId: {ArticleId}, Requested: {Quantity}, Available: {Available}", 
                item.ArticleId, item.Quantity, stock?.Quantity ?? 0);
            throw new BusinessException("Insufficient stock");
        }
    }
    
    // Business logic: Calculate total
    decimal total = CalculateOrderTotal(createDto);
    _logger.LogInformation("Order total calculated. Total: {Total}", total);
    
    Order order = createDto.Adapt<Order>();
    order.Total = total;
    await _orderRepository.AddAsync(order, cancellationToken);
    await // Dapper commits immediately(cancellationToken);
    
    _logger.LogInformation("Order created successfully. OrderId: {OrderId}, Total: {Total}", order.Id, total);
    return order.Adapt<OrderDto>();
}
```

#### Log Level Guidelines

- `LogInformation`: Business operations, domain events, successful validations
- `LogWarning`: Business rule violations, recoverable business errors
- `LogError`: Business logic exceptions, unrecoverable errors

---

### 3. Infrastructure Layer (Data Access)

**Responsibility**: Database operations, data persistence, external service calls

#### ✅ MUST Log

1. **Database Errors and Exceptions**
   - SQL exceptions
   - Constraint violations
   - Connection failures
   - Transaction failures
   - Concurrency conflicts

2. **Performance Issues**
   - Slow queries (exceeding threshold, e.g., > 1000ms)
   - Query timeouts
   - Connection pool exhaustion

3. **Critical Operations**
   - Bulk operations (audit trail)
   - Transaction operations (begin, commit, rollback)
   - Data migrations
   - Schema changes

4. **External Service Calls**
   - HTTP client errors
   - Timeouts
   - Retry attempts

#### ❌ MUST NOT Log

- Simple CRUD operations (OpenTelemetry handles query tracing)
- Normal successful queries
- Information already logged at Service/Controller layers
- Every GetById/GetAll call

#### When to Log

| Operation Type | Log? | Reason |
|---------------|------|--------|
| Simple GetById/GetAll | ❌ No | OpenTelemetry traces queries automatically |
| Simple Create/Update/Delete | ❌ No | OpenTelemetry traces queries automatically |
| Database errors | ✅ Yes | Always log exceptions |
| Slow queries | ✅ Yes | Performance monitoring |
| Bulk operations | ✅ Yes | Audit trail |
| Transactions | ✅ Yes | Critical operations |
| Complex queries | ⚠️ Only if slow/fails | Performance/error tracking |

#### Example: Simple CRUD (No Logging)

```csharp
// ❌ NO LOGGING - OpenTelemetry handles query tracing
public override async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
{
    IDbConnection connection = GetReadConnection();
    try
    {
        string sql = "SELECT id AS Id, firstname AS FirstName FROM webshop.customers WHERE id = @Id AND isactive = true";
        return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { Id = id });
    }
    finally
    {
        connection.Dispose();
    }
}
```

#### Example: Error Handling (Logging Required)

```csharp
// ✅ MUST LOG - Database errors
public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
{
    IDbConnection connection = GetWriteConnection();
    IDbTransaction? transaction = _transactionManager?.GetCurrentTransaction();
    
    try
    {
        string sql = DapperQueryBuilder.BuildUpdateQuery(TableName, GetUpdateColumns());
        int rowsAffected = await connection.ExecuteAsync(
            new CommandDefinition(sql, entity, transaction, cancellationToken: cancellationToken));
            
        if (rowsAffected == 0)
        {
            _logger.LogWarning("Update failed: Entity not found. EntityType: {EntityType}, Id: {Id}", 
                typeof(T).Name, entity.Id);
            throw new InvalidOperationException($"Entity with Id {entity.Id} not found");
        }
    }
    catch (NpgsqlException ex)
    {
        _logger.LogError(ex, "Database error during update. EntityType: {EntityType}, Error: {Error}", 
            typeof(T).Name, ex.Message);
        throw;
    }
    finally
    {
        if (transaction == null)
        {
            connection.Dispose();
        }
    }
}
```

#### Example: Performance Monitoring (Logging Required)

```csharp
// ✅ MUST LOG - Slow queries
public async Task<List<Customer>> GetCustomersWithOrdersAsync(CancellationToken cancellationToken = default)
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    try
    {
        var customers = await connection.QueryAsync
            
            c => c.Orders)
            .ToListAsync(cancellationToken);
        
        stopwatch.Stop();
        
        // Log if query is slow (threshold: 1000ms)
        if (stopwatch.ElapsedMilliseconds > 1000)
        {
            _logger.LogWarning("Slow query detected. GetCustomersWithOrdersAsync took {ElapsedMs}ms", 
                stopwatch.ElapsedMilliseconds);
        }
        
        return customers;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Database error in GetCustomersWithOrdersAsync");
        throw;
    }
}
```

#### Example: Bulk Operations (Logging Required)

```csharp
// ✅ MUST LOG - Bulk operations (audit trail)
public async Task<int> BulkUpdateStatusAsync(List<int> customerIds, bool isActive, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Bulk updating customer status. Count: {Count}, IsActive: {IsActive}", 
        customerIds.Count, isActive);
    
    try
    {
        var customers = await connection.QueryAsync
            .Where(c => customerIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
        
        foreach (var customer in customers)
        {
            customer.IsActive = isActive;
            customer.UpdatedAt = DateTime.UtcNow;
        }
        
        await _transactionManager.CommitTransactionAsync(cancellationToken);
        
        _logger.LogInformation("Bulk update completed. Updated {Count} customers", customers.Count);
        return customers.Count;
    }
    catch (DbUpdateException ex)
    {
        _logger.LogError(ex, "Database error during bulk update. CustomerIds: {CustomerIds}", 
            string.Join(", ", customerIds));
        throw;
    }
}
```

#### Log Level Guidelines

- `LogInformation`: Bulk operations, transactions (audit trail)
- `LogWarning`: Slow queries, concurrency conflicts, retry attempts
- `LogError`: Database errors, connection failures, constraint violations

#### OpenTelemetry Database Instrumentation

**Important**: OpenTelemetry provides automatic query tracing via Npgsql instrumentation. Configure in `Program.cs`:

```csharp
services.AddOpenTelemetry()
    .WithTracing(tracing =>
    {
        tracing.AddNpgsqlInstrumentation(options =>
        {
            options.EnableConnectionLevelAttributes = true;
            options.SetDbStatementForText = true; // Log SQL statements
        });
    });
```

**OpenTelemetry Automatically Tracks:**

- ✅ SQL queries and parameters (when configured)
- ✅ Query execution time
- ✅ Database connection events
- ✅ Connection pool metrics

**Recommendation**: Rely on OpenTelemetry for normal query tracing. Only add explicit logging for errors, performance issues, and critical operations.

---

## Decision Matrix: What to Log When

| Scenario | Controller | Service | Infrastructure |
|----------|-----------|---------|----------------|
| **Simple GET Request** | ❌ No logging (OpenTelemetry) | ❌ No logging | ❌ No logging (OpenTelemetry) |
| **Simple POST Request** | ❌ No logging (OpenTelemetry + Service) | ✅ Log business event | ❌ No logging (OpenTelemetry) |
| **GET Request - Not Found** | ✅ Log 404 (HTTP error) | ❌ No logging | ❌ No logging (OpenTelemetry) |
| **Complex Business Operation** | ❌ No logging (OpenTelemetry + Service) | ✅ Log business logic | ⚠️ Log if slow/fails |
| **Database Error** | ❌ No logging (Middleware) | ✅ Log service error | ✅ Log database error |
| **Slow Query** | ❌ No logging | ❌ No logging | ✅ Log performance issue |
| **Bulk Operation** | ❌ No logging (OpenTelemetry + Service) | ✅ Log business event | ✅ Log audit trail |
| **Transaction Failure** | ❌ No logging (Middleware) | ✅ Log service error | ✅ Log transaction error |
| **Business Rule Violation** | ❌ No logging (Middleware) | ✅ Log validation failure | ❌ No logging |

---

## Complete Request Flow Examples

### Example 1: Simple GET Request

**Request**: `GET /api/v1/customers/123`

**Log Output:**

```
[INFO] CustomerController: HTTP GET /api/v1/customers/123 - Request received
[INFO] CustomerController: HTTP GET /api/v1/customers/123 - Response 200 OK
```

**What Happened:**

- ✅ Controller logged HTTP request and response
- ❌ Service: No logging (simple pass-through)
- ❌ Infrastructure: No logging (OpenTelemetry handles tracing)

**This is correct** - No duplication, clear observability.

---

### Example 2: Create Request with Business Logic

**Request**: `POST /api/v1/orders`

**Log Output:**

```
[INFO] OpenTelemetry: http.method=POST, http.route=/api/v1/orders, http.status_code=201
[INFO] OrderService: Creating order with business validation. CustomerId: 456, ItemsCount: 3
[INFO] OrderService: Order created successfully. OrderId: 789, Total: 299.99
```

**What Happened:**

- ❌ Controller: No logging (OpenTelemetry captures HTTP, Service logs business event)
- ✅ Service logged business logic and domain event
- ❌ Infrastructure: No logging (OpenTelemetry handles tracing)

**This is correct** - No duplication, each layer logs only its concerns.

---

### Example 3: Request with Database Error

**Request**: `POST /api/v1/orders`

**Log Output:**

```
[INFO] OpenTelemetry: http.method=POST, http.route=/api/v1/orders, http.status_code=500
[INFO] OrderService: Creating order with business validation. CustomerId: 456, ItemsCount: 3
[ERROR] OrderRepository: Database error during SaveChanges. EntityType: Order, Error: Foreign key constraint violation
[ERROR] OrderService: Order creation failed. CustomerId: 456
[ERROR] ExceptionHandlingMiddleware: Exception occurred. ErrorId: {guid}
```

**What Happened:**

- ❌ Controller: No logging (ExceptionHandlingMiddleware handles HTTP errors)
- ✅ Service logged business logic failure
- ✅ Infrastructure logged database error
- ✅ Middleware logged HTTP error response

**This is correct** - Each layer logs errors from its perspective, no duplication.

---

### Example 4: Request with Slow Query

**Request**: `GET /api/v1/customers/123/orders`

**Log Output:**

```
[INFO] OpenTelemetry: http.method=GET, http.route=/api/v1/customers/{id}/orders, route.id=123, http.status_code=200, durationMs=1250
[WARNING] OrderRepository: Slow query detected. GetOrdersByCustomerIdAsync took 1250ms
```

**What Happened:**

- ❌ Controller: No logging (OpenTelemetry captures HTTP and duration)
- ❌ Service: No logging (simple pass-through)
- ✅ Infrastructure logged performance issue

**This is correct** - Performance monitoring at the appropriate layer, no duplication.

---

## Structured Logging Requirements

### MUST Use Structured Logging

All log statements **MUST** use structured logging with named parameters:

```csharp
// ✅ CORRECT - Structured logging
_logger.LogInformation("Creating customer. CustomerId: {CustomerId}, Email: {Email}", customerId, email);

// ❌ INCORRECT - String interpolation
_logger.LogInformation($"Creating customer. CustomerId: {customerId}, Email: {email}");

// ❌ INCORRECT - String concatenation
_logger.LogInformation("Creating customer. CustomerId: " + customerId + ", Email: " + email);
```

### MUST Include Context Identifiers

Always include relevant identifiers for correlation:

- `{CustomerId}`, `{OrderId}`, `{ProductId}` - Entity identifiers
- `{RequestId}` - Request correlation ID (if available)
- `{UserId}` - User identifier (if available)
- `{ElapsedMs}` - Performance metrics

---

## Log Level Guidelines

### LogInformation

- Normal workflow events
- Successful operations
- Important state changes
- HTTP requests/responses
- Business events

### LogWarning

- Recoverable issues
- Business rule violations
- Missing optional data
- Slow queries
- 4xx HTTP responses
- Concurrency conflicts

### LogError

- Exceptions and failures
- Unrecoverable errors
- Database errors
- 5xx HTTP responses
- System failures

### LogDebug

- Detailed diagnostic information
- Development-only logging
- Verbose operation details
- **Note**: Usually disabled in production

---

## Anti-Patterns: What NOT to Do

### ❌ Anti-Pattern 1: Duplicate Logging

```csharp
// Controller
_logger.LogInformation("Retrieving customer by ID. CustomerId: {CustomerId}", id);

// Service - DUPLICATE!
_logger.LogInformation("Retrieving customer by ID. CustomerId: {CustomerId}", id);
```

**Problem**: Same log message at multiple layers creates noise.

**Solution**: Log only at Controller layer for simple operations.

---

### ❌ Anti-Pattern 2: Logging at Wrong Layer

```csharp
// Service - WRONG! HTTP concerns don't belong here
_logger.LogInformation("HTTP GET /api/v1/customers/{CustomerId} - Request received", id);
```

**Problem**: Service layer logging HTTP concerns violates separation of concerns.

**Solution**: Move HTTP logging to Controller layer.

---

### ❌ Anti-Pattern 3: Over-Logging in Infrastructure

```csharp
// Repository - TOO VERBOSE!
public override async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
{
    _logger.LogInformation("Getting customer by ID. CustomerId: {CustomerId}", id); // ❌ Unnecessary
    IDbConnection connection = GetReadConnection();
    try
    {
        string sql = "SELECT id AS Id, firstname AS FirstName FROM webshop.customers WHERE id = @Id";
        Customer? customer = await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { Id = id });
        _logger.LogInformation("Customer retrieved. CustomerId: {CustomerId}, Found: {Found}", id, customer != null); // ❌ Unnecessary
        return customer;
    }
    finally
    {
        connection.Dispose();
    }
}
```

**Problem**: Logging every simple operation creates log noise. OpenTelemetry already traces queries.

**Solution**: Only log errors, slow queries, and critical operations.

---

### ❌ Anti-Pattern 4: Missing Error Logging

```csharp
// Repository - MISSING ERROR HANDLING!
public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
{
    IDbConnection connection = GetWriteConnection();
    string sql = DapperQueryBuilder.BuildUpdateQuery(TableName, GetUpdateColumns());
    await connection.ExecuteAsync(sql, entity); // ❌ No error logging or disposal
}
```

**Problem**: Database errors won't be logged, making debugging difficult. Also missing proper connection disposal.

**Solution**: Always wrap database operations in try-catch, log errors, and dispose connections properly.

---

## Quick Reference: Decision Tree

```
Request comes in
  ↓
Controller: Log HTTP request? → ❌ NO (OpenTelemetry handles this)
  ↓
Service: Has business logic? → ❌ NO → Skip Service logging
  ↓                        → ✅ YES → Log business logic
  ↓
Repository: Operation simple? → ✅ YES → Skip Infrastructure logging
  ↓                        → ❌ NO → Log if slow/error/bulk
  ↓
Controller: Log HTTP response? → ❌ NO (OpenTelemetry handles this)
  ↓
Controller: HTTP error (404)? → ✅ YES → Log HTTP-level error outcome
```

---

## Summary: What to Log When

### Controller Layer

- ✅ **When**: HTTP-level error outcomes (404 Not Found)
- ❌ **Never**: HTTP technical details (OpenTelemetry handles this)
- ❌ **Never**: Business operations (Business Service handles this)
- ❌ **Never**: Success messages (status codes indicate success)
- ❌ **Never**: Business logic details
- ❌ **Never**: Data access operations

### Service Layer

- ✅ **When**: Complex business logic operations
- ✅ **When**: Business rule validations
- ✅ **When**: Domain events (Create/Update/Delete)
- ❌ **Never**: Simple CRUD pass-through
- ❌ **Never**: HTTP concerns

### Infrastructure Layer

- ✅ **Always**: Database errors and exceptions
- ✅ **When**: Slow queries (performance monitoring)
- ✅ **When**: Bulk operations (audit trail)
- ✅ **When**: Transaction operations
- ❌ **Never**: Simple CRUD (OpenTelemetry handles)
- ❌ **Never**: Normal successful queries

---

## References

- [Microsoft: Logging in .NET](https://learn.microsoft.com/en-us/dotnet/core/extensions/logging)
- [Microsoft: ASP.NET Core Logging](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/)
- [OpenTelemetry .NET](https://opentelemetry.io/docs/languages/net/)
- [Clean Architecture Best Practices](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [Structured Logging Best Practices](https://www.honeycomb.io/blog/best-practices-for-structured-logging/)

---
