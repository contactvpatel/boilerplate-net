# XML Comments and InheritDoc Guidelines

## Overview

This document provides comprehensive guidelines for writing XML documentation comments in C# code, including when and how to use `/// <inheritdoc />` to inherit documentation from interfaces, base classes, or overridden members.

[← Back to README](../../README.md)

## Table of Contents

- [Why XML Comments Matter](#why-xml-comments-matter)
- [Basic XML Comment Structure](#basic-xml-comment-structure)
- [When to Use XML Comments](#when-to-use-xml-comments)
- [Self-Explanatory Code: The Foundation](#self-explanatory-code-the-foundation)
- [When to Use `/// <inheritdoc />`](#when-to-use-inheritdoc)
- [When NOT to Use `/// <inheritdoc />`](#when-not-to-use-inheritdoc)
- [Best Practices](#best-practices)
- [Examples from Codebase](#examples-from-codebase)
- [API Controller Documentation Patterns](#api-controller-documentation-patterns)
- [Common Patterns](#common-patterns)
- [Troubleshooting](#troubleshooting)

---

## Why XML Comments Matter

### Benefits

1. **IntelliSense Support**: XML comments appear in IDE tooltips, improving developer experience
2. **API Documentation**: Auto-generated documentation (e.g., via DocFX, Sandcastle)
3. **Code Maintainability**: Helps developers understand code without reading implementation
4. **Contract Clarity**: Documents expected behavior, parameters, and return values
5. **Onboarding**: New team members can understand code faster

### Impact

- **Without XML Comments**: Developers must read implementation to understand usage
- **With XML Comments**: Developers get immediate context in IDE tooltips
- **With `inheritdoc`**: Reduces duplication while maintaining documentation

---

## Basic XML Comment Structure

### Standard Tags

```csharp
/// <summary>
/// Brief description of the member (one sentence preferred).
/// </summary>
/// <param name="parameterName">Description of the parameter.</param>
/// <returns>Description of the return value.</returns>
/// <remarks>
/// Additional detailed information, examples, or notes.
/// </remarks>
/// <exception cref="ExceptionType">When this exception is thrown.</exception>
/// <example>
/// <code>
/// var result = MethodName(parameter);
/// </code>
/// </example>
```

### Common Tags

| Tag | Purpose | Required? |
|-----|---------|-----------|
| `<summary>` | Brief description | ✅ Yes (for public members) |
| `<param>` | Parameter description | ✅ Yes (if method has parameters) |
| `<returns>` | Return value description | ✅ Yes (if method returns a value) |
| `<remarks>` | Additional details | ⚠️ Optional |
| `<exception>` | Exception documentation | ⚠️ Optional |
| `<example>` | Usage examples | ⚠️ Optional |
| `<see cref="Type"/>` | Reference to another type | ⚠️ Optional |
| `<seealso cref="Type"/>` | Related types | ⚠️ Optional |

---

## When to Use XML Comments

### ✅ MUST Document

1. **All Public Members**
   - Public classes, interfaces, methods, properties
   - Public constructors
   - Public events

2. **Protected Members**
   - Protected methods (used by derived classes)
   - Protected properties

3. **Internal Members** (if part of public API)
   - Internal classes used across assemblies
   - Internal methods exposed to other projects

### ⚠️ SHOULD Document

1. **Complex Private Methods**
   - Non-obvious algorithms
   - Business logic that needs explanation

2. **Non-Standard Implementations**
   - Methods with side effects
   - Methods with performance implications

### ❌ DON'T Need to Document

1. **Simple Private Methods**
   - Obvious implementations
   - Self-explanatory code

2. **Auto-Properties**
   - Simple getters/setters without logic

3. **Obvious Constructors**
   - Parameterless constructors
   - Simple dependency injection constructors

---

## Self-Explanatory Code: The Foundation

### Why Self-Explanatory Code Matters

**Self-explanatory code reduces the need for extensive documentation.** Well-written code should communicate its intent clearly through:
- Meaningful names for classes, methods, and variables
- Clear structure and organization
- Obvious logic flow
- Appropriate abstractions

### The Balance: Code vs. Documentation

**Principle**: Write self-explanatory code first, then add documentation for what the code cannot express.

```csharp
// ❌ Bad - Code is unclear, needs extensive documentation
/// <summary>
/// Processes the data.
/// </summary>
/// <param name="d">The data to process.</param>
/// <returns>The processed data.</returns>
public Data Process(Data d)
{
    var r = new Data();
    for (int i = 0; i < d.Items.Count; i++)
    {
        if (d.Items[i].Status == 1)
        {
            r.Items.Add(d.Items[i]);
        }
    }
    return r;
}

// ✅ Good - Self-explanatory code, minimal documentation needed
/// <summary>
/// Filters active orders from the collection.
/// </summary>
public List<Order> GetActiveOrders(List<Order> orders)
{
    var activeOrders = new List<Order>();
    foreach (var order in orders)
    {
        if (order.Status == OrderStatus.Active)
        {
            activeOrders.Add(order);
        }
    }
    return activeOrders;
}
```

### Guidelines for Self-Explanatory Code

#### 1. **Use Meaningful Names**

```csharp
// ❌ Bad - Unclear names
public void Process(int id, string data) { }

// ✅ Good - Self-explanatory names
public void UpdateCustomerProfile(int customerId, string profileData) { }
```

#### 2. **Use Enums Instead of Magic Numbers**

```csharp
// ❌ Bad - Magic numbers need documentation
if (order.Status == 1) { }

// ✅ Good - Self-explanatory enum
if (order.Status == OrderStatus.Active) { }
```

#### 3. **Extract Complex Logic into Named Methods**

```csharp
// ❌ Bad - Complex logic in one method
public decimal CalculateTotal(Order order)
{
    decimal total = 0;
    foreach (var item in order.Items)
    {
        total += item.Price * item.Quantity;
        if (item.IsDiscounted)
        {
            total -= item.DiscountAmount;
        }
    }
    if (order.Customer.IsPremium)
    {
        total *= 0.9m; // 10% discount
    }
    total += order.ShippingCost;
    return total;
}

// ✅ Good - Self-explanatory method names
public decimal CalculateTotal(Order order)
{
    decimal subtotal = CalculateSubtotal(order.Items);
    decimal discount = CalculateCustomerDiscount(subtotal, order.Customer);
    return subtotal - discount + order.ShippingCost;
}

private decimal CalculateSubtotal(List<OrderItem> items)
{
    return items.Sum(item => item.Price * item.Quantity - item.DiscountAmount);
}

private decimal CalculateCustomerDiscount(decimal amount, Customer customer)
{
    return customer.IsPremium ? amount * 0.1m : 0;
}
```

#### 4. **Use Descriptive Variable Names**

```csharp
// ❌ Bad - Single letter or unclear names
var x = GetData();
var temp = Process(x);
return temp;

// ✅ Good - Descriptive names
var customerData = GetCustomerData();
var processedCustomer = ProcessCustomerData(customerData);
return processedCustomer;
```

#### 5. **Avoid Deep Nesting**

```csharp
// ❌ Bad - Deep nesting is hard to understand
public void ProcessOrder(Order order)
{
    if (order != null)
    {
        if (order.Items != null)
        {
            if (order.Items.Count > 0)
            {
                foreach (var item in order.Items)
                {
                    if (item.IsValid)
                    {
                        // Process item
                    }
                }
            }
        }
    }
}

// ✅ Good - Early returns and guard clauses
public void ProcessOrder(Order order)
{
    if (order?.Items == null || order.Items.Count == 0)
        return;

    foreach (var item in order.Items.Where(i => i.IsValid))
    {
        ProcessOrderItem(item);
    }
}
```

### When Self-Explanatory Code Isn't Enough

Even with self-explanatory code, you still need XML comments for:

1. **Public API Contracts**
   - What the method does (summary)
   - Parameter constraints and requirements
   - Return value semantics
   - Exception conditions

2. **Business Rules**
   - Why certain logic exists
   - Business context and constraints
   - Domain-specific behavior

3. **Performance Considerations**
   - Time complexity
   - Caching behavior
   - Resource usage

4. **Side Effects**
   - What else happens besides the return value
   - State changes
   - External dependencies

### Example: Self-Explanatory Code + Documentation

```csharp
// ✅ Good - Self-explanatory code with appropriate documentation
/// <summary>
/// Validates a JWT token with the SSO service using cache-first strategy.
/// </summary>
/// <param name="token">The JWT token to validate. Must not be null or empty.</param>
/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
/// <returns>True if the token is valid, false otherwise.</returns>
/// <remarks>
/// This method uses a cache-first strategy to reduce SSO service load.
/// Valid tokens are cached for 5 minutes. Invalid tokens are cached for
/// 30 seconds to prevent brute force attacks.
/// </remarks>
public async Task<bool> ValidateTokenWithCachingAsync(
    string token, 
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(token))
        throw new ArgumentException("Token cannot be null or empty", nameof(token));

    string cacheKey = GenerateCacheKey(token);
    
    return await _cache.GetOrCreateAsync(
        cacheKey,
        async cancel => await _ssoService.ValidateTokenAsync(token, cancel),
        expiration: TimeSpan.FromMinutes(5),
        cancellationToken: cancellationToken);
}
```

**Why this works**:
- Method name clearly states what it does (`ValidateTokenWithCachingAsync`)
- Parameter names are clear (`token`, `cancellationToken`)
- Logic flow is obvious (check cache, validate if needed)
- XML comments add **why** (caching strategy) and **constraints** (parameter requirements)

### Best Practice: Code First, Documentation Second

1. **Write self-explanatory code** - Use clear names, structure, and logic
2. **Add XML comments** - Document what the code cannot express:
   - Public API contracts
   - Business rules and context
   - Performance characteristics
   - Side effects and constraints
3. **Use `inheritdoc`** - When implementation matches documented contract

### Checklist: Is Your Code Self-Explanatory?

Before writing extensive XML comments, ask:
- [ ] Can a developer understand what the method does from its name?
- [ ] Are variable names descriptive and meaningful?
- [ ] Is the logic flow clear without comments?
- [ ] Are magic numbers replaced with named constants or enums?
- [ ] Is complex logic broken into smaller, named methods?
- [ ] Are guard clauses and early returns used to reduce nesting?

If all answers are "yes", you may only need minimal XML comments (summary, parameters, returns).

---

## When to Use `/// <inheritdoc />`

### ✅ Use `inheritdoc` When:

#### 1. **Implementing Interface Members**

**Scenario**: Your implementation matches the interface contract exactly.

```csharp
// Interface (Core layer)
public interface IMisService
{
    /// <summary>
    /// Gets all departments for a division.
    /// </summary>
    /// <param name="divisionId">Division identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of departments.</returns>
    Task<IEnumerable<DepartmentModel>> GetAllDepartmentsAsync(int divisionId, CancellationToken cancellationToken = default);
}

// Implementation (Business layer)
public class MisService : IMisService
{
    /// <inheritdoc />
    public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        // Simple mapping - interface docs are sufficient
        var models = await _coreService.GetAllDepartmentsAsync(divisionId, cancellationToken);
        return models.Adapt<IEnumerable<DepartmentDto>>();
    }
}
```

**Why**: The interface documentation is accurate, and the implementation is a simple pass-through or mapping.

#### 2. **Overriding Base Class Members**

**Scenario**: Your override doesn't change the behavior significantly.

```csharp
// Base class
public abstract class RepositoryBase<T>
{
    /// <summary>
    /// Saves changes to the database.
    /// </summary>
    /// <returns>Number of affected records.</returns>
    public virtual async Task<int> SaveChangesAsync()
    {
        // Base implementation
    }
}

// Derived class
public class Repository<T> : RepositoryBase<T>
{
    /// <inheritdoc />
    public override async Task<int> SaveChangesAsync()
    {
        // Same behavior, just override for logging
        return await base.SaveChangesAsync();
    }
}
```

**Why**: The base class documentation still accurately describes the behavior.

#### 3. **Explicit Interface Implementation**

**Scenario**: Simple explicit interface implementations.

```csharp
public class MyClass : IInterface
{
    /// <inheritdoc />
    void IInterface.Method()
    {
        // Implementation
    }
}
```

**Why**: Explicit implementations are usually straightforward pass-throughs.

#### 4. **Abstract Method Implementations**

**Scenario**: Implementing abstract methods from base classes.

```csharp
public abstract class ServiceBase
{
    /// <summary>
    /// Initializes the service.
    /// </summary>
    protected abstract Task InitializeAsync();
}

public class MyService : ServiceBase
{
    /// <inheritdoc />
    protected override async Task InitializeAsync()
    {
        // Implementation
    }
}
```

**Why**: The abstract method documentation describes the contract.

---

## When NOT to Use `/// <inheritdoc />`

### ❌ Don't Use `inheritdoc` When:

#### 1. **Implementation Adds Significant Behavior**

**Scenario**: Your implementation does more than the interface/base class.

```csharp
// ❌ Bad - Implementation adds caching
/// <inheritdoc />
public async Task<DepartmentDto> GetDepartmentAsync(int id)
{
    // Adds caching - should document this!
    return await _cache.GetOrCreateAsync($"dept-{id}", async () => 
    {
        return await _coreService.GetDepartmentAsync(id);
    });
}

// ✅ Good - Documents the additional behavior
/// <summary>
/// Gets a department by identifier with caching.
/// </summary>
/// <param name="id">Department identifier.</param>
/// <returns>Department if found, null otherwise.</returns>
/// <remarks>
/// This implementation caches results for 5 minutes to improve performance.
/// </remarks>
public async Task<DepartmentDto> GetDepartmentAsync(int id)
{
    return await _cache.GetOrCreateAsync($"dept-{id}", async () => 
    {
        return await _coreService.GetDepartmentAsync(id);
    });
}
```

#### 2. **Implementation Differs from Interface/Base**

**Scenario**: Parameters or behavior differ from the documented contract.

```csharp
// ❌ Bad - Return type differs
/// <inheritdoc />
public async Task<DepartmentDto> GetAllDepartmentsAsync(int divisionId)
{
    // Returns DTO, not Model - should document this difference
}

// ✅ Good - Documents the difference
/// <summary>
/// Gets all departments for a division, returning DTOs instead of models.
/// </summary>
/// <param name="divisionId">Division identifier.</param>
/// <returns>List of department DTOs.</returns>
/// <remarks>
/// This business layer implementation maps Core models to DTOs for API consumption.
/// </remarks>
public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync(int divisionId)
{
    // Implementation
}
```

#### 3. **Implementation Has Important Side Effects**

**Scenario**: Method has side effects not documented in base/interface.

```csharp
// ❌ Bad - Side effect not documented
/// <inheritdoc />
public async Task<bool> ValidateTokenAsync(string token)
{
    // Also logs security events - should document!
    _auditLogger.LogSecurityEvent(token);
    return await _ssoService.ValidateTokenAsync(token);
}

// ✅ Good - Documents side effects
/// <summary>
/// Validates a JWT token with the SSO service.
/// </summary>
/// <param name="token">The JWT token to validate.</param>
/// <returns>True if the token is valid, false otherwise.</returns>
/// <remarks>
/// This implementation logs all validation attempts to the security audit log
/// for compliance and security monitoring purposes.
/// </remarks>
public async Task<bool> ValidateTokenAsync(string token)
{
    _auditLogger.LogSecurityEvent(token);
    return await _ssoService.ValidateTokenAsync(token);
}
```

#### 4. **Implementation is Complex or Non-Obvious**

**Scenario**: Complex logic that needs explanation.

```csharp
// ❌ Bad - Complex algorithm needs explanation
/// <inheritdoc />
public decimal CalculatePrice(Order order)
{
    // Complex pricing logic with discounts, taxes, etc.
    // Should document the algorithm
}

// ✅ Good - Documents the complexity
/// <summary>
/// Calculates the total price for an order including discounts and taxes.
/// </summary>
/// <param name="order">The order to calculate price for.</param>
/// <returns>Total price including all adjustments.</returns>
/// <remarks>
/// Price calculation follows this order:
/// 1. Calculate base price from order items
/// 2. Apply customer discount (if applicable)
/// 3. Apply promotional discounts
/// 4. Calculate tax based on shipping address
/// 5. Apply shipping costs
/// </remarks>
public decimal CalculatePrice(Order order)
{
    // Implementation
}
```

#### 5. **No Documentation Exists to Inherit**

**Scenario**: Interface/base class has no XML comments.

```csharp
// Interface has no documentation
public interface IService
{
    void DoSomething(); // No XML comments
}

// ❌ Bad - Nothing to inherit
/// <inheritdoc />
public void DoSomething() { }

// ✅ Good - Add your own documentation
/// <summary>
/// Performs some operation.
/// </summary>
public void DoSomething() { }
```

---

## Best Practices

### 1. **Keep Summaries Concise**

```csharp
// ✅ Good - One clear sentence
/// <summary>
/// Gets all departments for a division.
/// </summary>

// ❌ Bad - Too verbose
/// <summary>
/// This method retrieves all departments that belong to a specific division
/// by querying the database and returning a list of department entities.
/// </summary>
```

### 2. **Document Parameters Clearly**

```csharp
// ✅ Good - Clear and specific
/// <param name="divisionId">The unique identifier of the division. Must be greater than 0.</param>

// ❌ Bad - Vague
/// <param name="divisionId">The division ID.</param>
```

### 3. **Document Return Values**

```csharp
// ✅ Good - Specific about return value
/// <returns>List of departments. Returns empty list if division has no departments.</returns>

// ❌ Bad - Vague
/// <returns>Departments.</returns>
```

### 4. **Use Remarks for Additional Context**

```csharp
/// <summary>
/// Validates a JWT token.
/// </summary>
/// <param name="token">The JWT token to validate.</param>
/// <returns>True if valid, false otherwise.</returns>
/// <remarks>
/// This method validates tokens using the SSO service. Results are cached
/// for 5 minutes to reduce SSO service load. Invalid tokens are cached for
/// 30 seconds to prevent brute force attacks.
/// </remarks>
```

### 5. **Use `inheritdoc` with Additional Remarks**

```csharp
/// <inheritdoc />
/// <remarks>
/// This implementation uses ASP.NET Core's structured logging,
/// which provides better integration with logging providers.
/// </remarks>
public void LogInformation(string format, params object[] args)
{
    _logger.LogInformation(string.Format(format, args));
}
```

### 6. **Document Exceptions**

```csharp
/// <summary>
/// Gets a department by identifier.
/// </summary>
/// <param name="id">Department identifier.</param>
/// <returns>Department if found, null otherwise.</returns>
/// <exception cref="ArgumentException">Thrown when id is less than or equal to 0.</exception>
/// <exception cref="InvalidOperationException">Thrown when database connection fails.</exception>
public async Task<DepartmentDto?> GetDepartmentAsync(int id)
{
    if (id <= 0)
        throw new ArgumentException("Id must be greater than 0", nameof(id));
    // Implementation
}
```

### 7. **Use `see` and `seealso` for References**

```csharp
/// <summary>
/// Gets departments using the <see cref="IMisService"/>.
/// </summary>
/// <seealso cref="DepartmentDto"/>
/// <seealso cref="DivisionDto"/>
```

### 8. **Use `<example>` Sections for Complex Endpoints**

For API controllers, especially complex endpoints, provide usage examples:

```csharp
/// <summary>
/// Gets orders within a date range.
/// </summary>
/// <param name="startDate">The start date of the range (inclusive). Format: ISO 8601 (e.g., 2024-01-01T00:00:00Z).</param>
/// <param name="endDate">The end date of the range (inclusive). Format: ISO 8601 (e.g., 2024-12-31T23:59:59Z).</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A list of orders created within the specified date range.</returns>
/// <remarks>
/// This endpoint retrieves all orders that were created between the start date and end date (inclusive).
/// Both dates are required query parameters. The end date must be greater than or equal to the start date.
/// Returns an empty list if no orders are found in the specified range.
/// </remarks>
/// <example>
/// <code>
/// GET /api/v1/orders/date-range?startDate=2024-01-01T00:00:00Z&amp;endDate=2024-12-31T23:59:59Z
/// </code>
/// </example>
```

### 9. **Document API Endpoint Behavior in `<remarks>`**

For API controllers, use `<remarks>` to explain:
- Endpoint behavior and requirements
- Parameter constraints and validation rules
- Response format and status codes
- Side effects (e.g., cache clearing, logging)
- Performance considerations

```csharp
/// <summary>
/// Logs out the current user and invalidates their token.
/// </summary>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>Success response if logout was successful, otherwise 401 Unauthorized.</returns>
/// <remarks>
/// This endpoint logs out the current authenticated user. It performs the following actions:
/// <list type="bullet">
/// <item><description>If the token is still valid, calls the SSO service to invalidate the token on the SSO server</description></item>
/// <item><description>If the token is already expired, skips the SSO API call to avoid unnecessary network traffic</description></item>
/// <item><description>Clears the cached JWT token validation result from the local cache</description></item>
/// <item><description>Clears all cached user-specific data (positions, ASM authorization, etc.)</description></item>
/// </list>
/// This endpoint is publicly accessible (no authentication required) to allow logout even with expired tokens.
/// </remarks>
/// <example>
/// <code>
/// POST /api/v1/sso/logout
/// Authorization: Bearer {access_token}
/// </code>
/// </example>
```

### 10. **Enhance Parameter Descriptions for API Endpoints**

For API controllers, provide detailed parameter descriptions including:
- Format requirements (e.g., ISO 8601 for dates)
- Constraints (e.g., "must be greater than 0")
- Default values
- Case sensitivity

```csharp
// ✅ Good - Detailed parameter description
/// <param name="id">The unique identifier of the customer (must be greater than 0).</param>
/// <param name="email">The email address of the customer (case-insensitive).</param>
/// <param name="startDate">The start date of the range (inclusive). Format: ISO 8601 (e.g., 2024-01-01T00:00:00Z).</param>
/// <param name="divisionId">The division identifier. Defaults to 1 if not provided or set to 0.</param>

// ❌ Bad - Vague parameter description
/// <param name="id">Customer ID.</param>
/// <param name="email">Email address.</param>
```

---

## Examples from Codebase

### Example 1: API Controller with Comprehensive Documentation

**File**: `src/WebShop.Api/Controllers/CustomerController.cs`

```csharp
/// <summary>
/// Creates a new customer.
/// </summary>
/// <param name="createDto">The customer creation data containing name, email, and other required fields.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>The newly created customer with generated ID, or 400 Bad Request if validation fails.</returns>
/// <remarks>
/// This endpoint creates a new customer in the system. The request body must contain all required fields as defined in <see cref="CreateCustomerDto"/>.
/// The email address must be unique. Upon successful creation, the response includes a Location header pointing to the new customer resource.
/// </remarks>
/// <example>
/// <code>
/// POST /api/v1/customers
/// {
///   "name": "John Doe",
///   "email": "john.doe@example.com",
///   "phone": "+1234567890"
/// }
/// </code>
/// </example>
[HttpPost]
[ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<Response<CustomerDto>>> Create([FromBody] CreateCustomerDto createDto, CancellationToken cancellationToken)
{
    // Implementation
}
```

**Why this is good**:
- Clear summary of what the endpoint does
- Detailed parameter description with context
- `<remarks>` explains behavior, requirements, and response headers
- `<example>` shows actual request format
- Cross-reference to DTO type using `<see cref>`

### Example 2: API Controller with Complex Query Parameters

**File**: `src/WebShop.Api/Controllers/OrderController.cs`

```csharp
/// <summary>
/// Gets orders within a date range.
/// </summary>
/// <param name="startDate">The start date of the range (inclusive). Format: ISO 8601 (e.g., 2024-01-01T00:00:00Z).</param>
/// <param name="endDate">The end date of the range (inclusive). Format: ISO 8601 (e.g., 2024-12-31T23:59:59Z).</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A list of orders created within the specified date range.</returns>
/// <remarks>
/// This endpoint retrieves all orders that were created between the start date and end date (inclusive).
/// Both dates are required query parameters. The end date must be greater than or equal to the start date.
/// Returns an empty list if no orders are found in the specified range.
/// </remarks>
/// <example>
/// <code>
/// GET /api/v1/orders/date-range?startDate=2024-01-01T00:00:00Z&amp;endDate=2024-12-31T23:59:59Z
/// </code>
/// </example>
[HttpGet("date-range")]
[ProducesResponseType(typeof(Response<IEnumerable<OrderDto>>), StatusCodes.Status200OK)]
public async Task<ActionResult<Response<IEnumerable<OrderDto>>>> GetByDateRange(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    CancellationToken cancellationToken)
{
    // Implementation
}
```

**Why this is good**:
- Parameter descriptions include format requirements (ISO 8601)
- `<remarks>` explains validation rules and behavior
- `<example>` shows URL-encoded query parameters
- Clear about inclusive date range behavior

### Example 3: API Controller with Complex Behavior

**File**: `src/WebShop.Api/Controllers/SsoController.cs`

```csharp
/// <summary>
/// Logs out the current user and invalidates their token.
/// </summary>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>Success response if logout was successful, otherwise 401 Unauthorized.</returns>
/// <remarks>
/// This endpoint logs out the current authenticated user. It performs the following actions:
/// <list type="bullet">
/// <item><description>If the token is still valid, calls the SSO service to invalidate the token on the SSO server</description></item>
/// <item><description>If the token is already expired, skips the SSO API call to avoid unnecessary network traffic</description></item>
/// <item><description>Clears the cached JWT token validation result from the local cache</description></item>
/// <item><description>Clears all cached user-specific data (positions, ASM authorization, etc.)</description></item>
/// </list>
/// This endpoint is publicly accessible (no authentication required) to allow logout even with expired tokens.
/// </remarks>
/// <example>
/// <code>
/// POST /api/v1/sso/logout
/// Authorization: Bearer {access_token}
/// </code>
/// </example>
[HttpPost("logout")]
[AllowAnonymous]
[ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(Response<bool>), StatusCodes.Status401Unauthorized)]
public async Task<ActionResult<Response<bool>>> Logout(CancellationToken cancellationToken)
{
    // Implementation
}
```

**Why this is good**:
- `<remarks>` uses `<list>` to clearly enumerate multiple side effects
- Documents conditional behavior (expired vs. valid tokens)
- Explains security considerations (publicly accessible)
- `<example>` shows required headers

### Example 4: Interface Implementation with `inheritdoc`

**File**: `src/WebShop.Business/Services/MisService.cs`

```csharp
// Interface (Core layer)
public interface IMisService
{
    /// <summary>
    /// Gets all departments for a division.
    /// </summary>
    /// <param name="divisionId">Division identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of departments.</returns>
    Task<IEnumerable<DepartmentModel>> GetAllDepartmentsAsync(int divisionId, CancellationToken cancellationToken = default);
}

// Implementation (Business layer)
public class MisService : IMisService
{
    /// <inheritdoc />
    public async Task<IEnumerable<DepartmentDto>> GetAllDepartmentsAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        IEnumerable<DepartmentModel> models = await _coreMisService.GetAllDepartmentsAsync(divisionId, cancellationToken);
        return models.Adapt<IEnumerable<DepartmentDto>>();
    }
}
```

**Why `inheritdoc` is appropriate**: 
- Simple mapping operation
- Interface documentation is accurate
- No additional behavior to document

### Example 5: Interface Implementation with Custom Documentation

**File**: `src/WebShop.Business/Services/ISsoService.cs`

```csharp
public interface ISsoService
{
    /// <summary>
    /// Validates a JWT token with the SSO service.
    /// </summary>
    /// <param name="token">The JWT token to validate.</param>
    /// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
    /// <returns>True if the token is valid, false otherwise.</returns>
    Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
}
```

**Implementation should use custom docs if it adds caching**:

```csharp
// ✅ Good - Documents caching behavior
/// <summary>
/// Validates a JWT token with the SSO service using cache-first strategy.
/// </summary>
/// <param name="token">The JWT token to validate.</param>
/// <param name="cancellationToken">Cancellation token to cancel the operation.</param>
/// <returns>True if the token is valid, false otherwise.</returns>
/// <remarks>
/// This implementation caches validation results for 5 minutes to reduce
/// SSO service load. Invalid tokens are cached for 30 seconds to prevent
/// brute force attacks.
/// </remarks>
public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
{
    // Implementation with caching
}
```

### Example 6: DbUp Logger Extension

**File**: `src/WebShop.Api/Extensions/DbUpLoggerExtension.cs`

```csharp
public class DbUpLoggerExtension(ILogger logger) : IUpgradeLog
{
    /// <inheritdoc />
    public void LogTrace(string format, params object[] args)
    {
        _logger.LogTrace(string.Format(format, args));
    }
}
```

**Why `inheritdoc` is appropriate**:
- Simple pass-through to `ILogger`
- Interface documentation is sufficient
- No additional behavior

**Optional enhancement**:

```csharp
/// <summary>
/// DbUp logger extension that integrates with ASP.NET Core ILogger.
/// </summary>
/// <remarks>
/// This class implements IUpgradeLog to bridge DbUp's logging interface
/// with ASP.NET Core's ILogger, enabling unified logging across the application.
/// All methods delegate to the injected ILogger instance.
/// </remarks>
public class DbUpLoggerExtension(ILogger logger) : IUpgradeLog
{
    /// <inheritdoc />
    public void LogTrace(string format, params object[] args)
    {
        _logger.LogTrace(string.Format(format, args));
    }
}
```

---

## API Controller Documentation Patterns

This section provides specific patterns for documenting ASP.NET Core API controllers, which have unique requirements compared to service layer code.

### Pattern 1: Simple GET Endpoint

For simple retrieval endpoints, provide basic documentation with optional remarks:

```csharp
/// <summary>
/// Gets all customers.
/// </summary>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A list of all customers in the system.</returns>
/// <remarks>
/// This endpoint retrieves all customers without any filtering. For large datasets, consider implementing pagination.
/// </remarks>
[HttpGet]
[ProducesResponseType(typeof(Response<IEnumerable<CustomerDto>>), StatusCodes.Status200OK)]
public async Task<ActionResult<Response<IEnumerable<CustomerDto>>>> GetAll(CancellationToken cancellationToken)
{
    // Implementation
}
```

### Pattern 2: GET Endpoint with Route Parameter

For endpoints with route parameters, include validation constraints and examples:

```csharp
/// <summary>
/// Gets a customer by ID.
/// </summary>
/// <param name="id">The unique identifier of the customer (must be greater than 0).</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>The customer if found, otherwise returns 404 Not Found.</returns>
/// <remarks>
/// This endpoint retrieves a single customer by their unique identifier. The ID must be a positive integer.
/// </remarks>
/// <example>
/// <code>
/// GET /api/v1/customers/123
/// </code>
/// </example>
[HttpGet("{id}")]
[ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status200OK)]
[ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status404NotFound)]
public async Task<ActionResult<Response<CustomerDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
{
    // Implementation
}
```

### Pattern 3: POST Endpoint (Create)

For creation endpoints, document request body format and response headers:

```csharp
/// <summary>
/// Creates a new customer.
/// </summary>
/// <param name="createDto">The customer creation data containing name, email, and other required fields.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>The newly created customer with generated ID, or 400 Bad Request if validation fails.</returns>
/// <remarks>
/// This endpoint creates a new customer in the system. The request body must contain all required fields as defined in <see cref="CreateCustomerDto"/>.
/// The email address must be unique. Upon successful creation, the response includes a Location header pointing to the new customer resource.
/// </remarks>
/// <example>
/// <code>
/// POST /api/v1/customers
/// {
///   "name": "John Doe",
///   "email": "john.doe@example.com",
///   "phone": "+1234567890"
/// }
/// </code>
/// </example>
[HttpPost]
[ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status201Created)]
[ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status400BadRequest)]
public async Task<ActionResult<Response<CustomerDto>>> Create([FromBody] CreateCustomerDto createDto, CancellationToken cancellationToken)
{
    // Implementation
}
```

### Pattern 4: PUT Endpoint (Update)

For update endpoints, document full update behavior and status codes:

```csharp
/// <summary>
/// Updates an existing customer.
/// </summary>
/// <param name="id">The unique identifier of the customer to update (must be greater than 0).</param>
/// <param name="updateDto">The customer update data containing fields to modify.</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>204 No Content if successful, 404 Not Found if customer doesn't exist, or 400 Bad Request if validation fails.</returns>
/// <remarks>
/// This endpoint performs a full update of the customer. All fields in <see cref="UpdateCustomerDto"/> should be provided.
/// The email address must remain unique if changed. This is a PUT operation, so partial updates are not supported.
/// </remarks>
/// <example>
/// <code>
/// PUT /api/v1/customers/123
/// {
///   "name": "John Doe Updated",
///   "email": "john.doe.updated@example.com",
///   "phone": "+1234567890"
/// }
/// </code>
/// </example>
[HttpPut("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status404NotFound)]
[ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCustomerDto updateDto, CancellationToken cancellationToken)
{
    // Implementation
}
```

### Pattern 5: DELETE Endpoint

For delete endpoints, document soft delete behavior and status codes:

```csharp
/// <summary>
/// Deletes a customer (soft delete).
/// </summary>
/// <param name="id">The unique identifier of the customer to delete (must be greater than 0).</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>204 No Content if successful, or 404 Not Found if customer doesn't exist.</returns>
/// <remarks>
/// This endpoint performs a soft delete on the customer. The customer record is marked as deleted but not physically removed from the database.
/// Soft-deleted customers are excluded from normal queries but can be recovered if needed.
/// </remarks>
/// <example>
/// <code>
/// DELETE /api/v1/customers/123
/// </code>
/// </example>
[HttpDelete("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
{
    // Implementation
}
```

### Pattern 6: Complex Query Endpoint

For endpoints with query parameters, document format requirements and validation rules:

```csharp
/// <summary>
/// Gets orders within a date range.
/// </summary>
/// <param name="startDate">The start date of the range (inclusive). Format: ISO 8601 (e.g., 2024-01-01T00:00:00Z).</param>
/// <param name="endDate">The end date of the range (inclusive). Format: ISO 8601 (e.g., 2024-12-31T23:59:59Z).</param>
/// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
/// <returns>A list of orders created within the specified date range.</returns>
/// <remarks>
/// This endpoint retrieves all orders that were created between the start date and end date (inclusive).
/// Both dates are required query parameters. The end date must be greater than or equal to the start date.
/// Returns an empty list if no orders are found in the specified range.
/// </remarks>
/// <example>
/// <code>
/// GET /api/v1/orders/date-range?startDate=2024-01-01T00:00:00Z&amp;endDate=2024-12-31T23:59:59Z
/// </code>
/// </example>
[HttpGet("date-range")]
[ProducesResponseType(typeof(Response<IEnumerable<OrderDto>>), StatusCodes.Status200OK)]
public async Task<ActionResult<Response<IEnumerable<OrderDto>>>> GetByDateRange(
    [FromQuery] DateTime startDate,
    [FromQuery] DateTime endDate,
    CancellationToken cancellationToken)
{
    // Implementation
}
```

---

## Common Patterns

### Pattern 1: Business Layer Wrapper

**Scenario**: Business layer wraps Core layer with DTO mapping.

```csharp
// Core Interface
public interface ICoreService
{
    /// <summary>
    /// Gets all items.
    /// </summary>
    Task<IEnumerable<Model>> GetAllAsync();
}

// Business Implementation
public class BusinessService : IBusinessService
{
    /// <summary>
    /// Gets all items as DTOs.
    /// </summary>
    /// <returns>List of item DTOs.</returns>
    /// <remarks>
    /// This implementation retrieves Core models and maps them to DTOs
    /// for API consumption.
    /// </remarks>
    public async Task<IEnumerable<Dto>> GetAllAsync()
    {
        var models = await _coreService.GetAllAsync();
        return models.Adapt<IEnumerable<Dto>>();
    }
}
```

**Decision**: Use custom docs because return type differs (DTOs vs Models).

### Pattern 2: Simple Pass-Through

**Scenario**: Implementation is a simple pass-through.

```csharp
public interface IService
{
    /// <summary>
    /// Performs an operation.
    /// </summary>
    Task DoSomethingAsync();
}

public class Service : IService
{
    /// <inheritdoc />
    public async Task DoSomethingAsync()
    {
        await _dependency.DoSomethingAsync();
    }
}
```

**Decision**: Use `inheritdoc` because it's a simple pass-through.

### Pattern 3: Implementation with Caching

**Scenario**: Implementation adds caching.

```csharp
public interface IService
{
    /// <summary>
    /// Gets data by identifier.
    /// </summary>
    Task<Data> GetByIdAsync(int id);
}

public class Service : IService
{
    /// <summary>
    /// Gets data by identifier with caching.
    /// </summary>
    /// <param name="id">Data identifier.</param>
    /// <returns>Data if found.</returns>
    /// <remarks>
    /// Results are cached for 10 minutes to improve performance.
    /// </remarks>
    public async Task<Data> GetByIdAsync(int id)
    {
        return await _cache.GetOrCreateAsync($"data-{id}", async () =>
        {
            return await _coreService.GetByIdAsync(id);
        });
    }
}
```

**Decision**: Use custom docs because caching is additional behavior.

---

## Troubleshooting

### Issue: `inheritdoc` Not Working

**Symptoms**: Documentation doesn't appear in IntelliSense.

**Solutions**:
1. Ensure interface/base class has XML comments
2. Check that XML documentation file generation is enabled in project:
   ```xml
   <PropertyGroup>
     <GenerateDocumentationFile>true</GenerateDocumentationFile>
   </PropertyGroup>
   ```
3. Rebuild the solution
4. Restart IDE

### Issue: Which Documentation is Inherited?

**Priority Order** (highest to lowest):
1. Interface documentation (if implementing interface)
2. Base class documentation (if overriding)
3. Overridden member documentation

**Example**:
```csharp
// Base class
public class Base
{
    /// <summary>Base method.</summary>
    public virtual void Method() { }
}

// Interface
public interface IInterface
{
    /// <summary>Interface method.</summary>
    void Method();
}

// Implementation
public class Derived : Base, IInterface
{
    /// <inheritdoc />
    public void Method() { }
}
```

**Result**: Inherits from interface (higher priority).

### Issue: Partial Documentation Inheritance

**Scenario**: Want to inherit some tags but override others.

**Solution**: Use `inheritdoc` with additional tags:

```csharp
/// <inheritdoc />
/// <remarks>
/// This implementation adds caching for improved performance.
/// </remarks>
public async Task<Data> GetByIdAsync(int id)
{
    // Implementation
}
```

This inherits `<summary>`, `<param>`, `<returns>` from interface, but adds custom `<remarks>`.

---

## Summary

### Quick Decision Tree

```
Is the member public?
├─ No → Document only if complex
└─ Yes → Continue

Does it implement/override something?
├─ No → Write full XML comments
└─ Yes → Continue

Is implementation simple (pass-through/mapping)?
├─ No → Write custom XML comments
└─ Yes → Continue

Does interface/base have documentation?
├─ No → Write full XML comments
└─ Yes → Use /// <inheritdoc />
```

### Rules of Thumb

1. **Public members**: Always document
2. **Simple implementations**: Use `inheritdoc`
3. **Complex implementations**: Write custom docs
4. **Additional behavior**: Document it
5. **No base docs**: Write your own

### Checklist

Before using `inheritdoc`, verify:
- [ ] Interface/base class has XML comments
- [ ] Implementation matches the documented contract
- [ ] No significant additional behavior
- [ ] Return types match (or difference is obvious)
- [ ] Parameters match (or difference is obvious)

---

## Related Documentation

- [C# XML Documentation Comments](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/)
- [InheritDoc Tag](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/xmldoc/recommended-tags#inheritdoc)
- [.NET Documentation Standards](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md)

---

## Change Log

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-12-27 | 1.0 | Initial guideline document created | System |
| 2025-12-27 | 1.1 | Added API Controller documentation patterns, enhanced examples with `<remarks>` and `<example>` sections, added best practices for parameter descriptions in API endpoints | System |

