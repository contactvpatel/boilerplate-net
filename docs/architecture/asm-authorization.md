# ASM Authorization Guide

This guide explains how to use the ASM (Application Security Management) authorization system in the WebShop API, including support for multiple permissions with OR and AND logical conditions.

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Basic Usage](#basic-usage)
- [Multiple Permissions](#multiple-permissions)
- [Logical Operators](#logical-operators)
- [Permission Requirements](#permission-requirements)
- [Usage Examples](#usage-examples)
- [Configuration](#configuration)
- [Error Responses](#error-responses)

---

## Overview

The ASM authorization system provides fine-grained access control based on user roles and permissions retrieved from the ASM service. It supports:

- **Single Permission Checks**: Simple authorization for specific operations
- **Multiple Permissions**: Check for multiple permissions at once
- **Logical Operators**: OR and AND conditions between permissions
- **Modular Architecture**: Separated validation and response creation logic
- **Flexible Configuration**: Easy to extend and customize

## Architecture

The ASM authorization system follows a modular architecture with clear separation of concerns:

```
┌─────────────────┐    ┌───────────────────┐    ┌─────────────────┐
│ AsmAuthorization │────│ AsmAuthorization  │────│ ASM Service     │
│   Attribute      │    │   Validation      │    │ (External)      │
│                 │    │   Filter          │    │                 │
└─────────────────┘    └───────────────────┘    └─────────────────┘
                              │                       │
                              ▼                       ▼
                       ┌───────────────────┐    ┌───────────────────┐
                       │ IAsmPermission    │    │   User Context    │
                       │   Validator       │    │   (JWT Token)     │
                       └───────────────────┘    └───────────────────┘
                              │
                              ▼
                       ┌───────────────────┐
                       │ IAsmErrorResponse │
                       │   Factory         │
                       └───────────────────┘
```

### Components

**1. AsmAuthorization Attribute**
- Declarative permission requirements on controllers/actions
- Support for single or multiple permissions with logical operators
- TypeFilterAttribute implementation for dependency injection

**2. AsmAuthorizationValidation Filter**
- Orchestrates the authorization process using injected dependencies
- Performs runtime configuration check for ASM authorization enablement
- Coordinates between permission validator and error response factory
- Handles user context validation and comprehensive logging

**3. IAsmPermissionValidator**
- Validates permissions against ASM service data
- Implements OR/AND logic for complex permission requirements
- Checks specific permission patterns (MODULE:ACTION)

**4. IAsmErrorResponseFactory**
- Creates standardized error responses for different scenarios
- Handles HTTP status codes (401 Unauthorized, 403 Forbidden, 500 Internal Server Error)
- Formats responses using the standard Response<T> structure

---

## Basic Usage

### Single Permission

```csharp
[ApiController]
[Route("api/v1/customers")]
public class CustomerController : BaseApiController
{
    [HttpGet]
    [AsmAuthorization(ModuleCode.Customer, AccessType.View)]
    public async Task<IActionResult> GetAll()
    {
        // User must have VIEW permission for CUSTOMER module
        var customers = await _customerService.GetAllAsync();
        return Ok(Response<IReadOnlyList<CustomerDto>>.Success(customers));
    }

    [HttpPost]
    [AsmAuthorization(ModuleCode.Customer, AccessType.Create)]
    public async Task<IActionResult> Create(CreateCustomerDto dto)
    {
        // User must have CREATE permission for CUSTOMER module
        var customer = await _customerService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id },
            Response<CustomerDto>.Success(customer));
    }
}
```

---

## Multiple Permissions

### OR Logic (Default)

User must have **at least one** of the specified permissions:

```csharp
[HttpGet("reports")]
[AsmAuthorization(new[]
{
    new PermissionRequirement(ModuleCode.Customer, AccessType.View),
    new PermissionRequirement(ModuleCode.Order, AccessType.View)
}, LogicalOperator.OR)]
public async Task<IActionResult> GetReports()
{
    // User can access if they have either CUSTOMER:VIEW OR ORDER:VIEW permission
    var reports = await _reportService.GenerateReportsAsync();
    return Ok(Response<ReportDto>.Success(reports));
}
```

### AND Logic

User must have **all** of the specified permissions:

```csharp
[HttpPost("bulk-update")]
[AsmAuthorization(new[]
{
    new PermissionRequirement(ModuleCode.Product, AccessType.Update),
    new PermissionRequirement(ModuleCode.Stock, AccessType.Update)
}, LogicalOperator.AND)]
public async Task<IActionResult> BulkUpdateProducts(BulkUpdateDto dto)
{
    // User must have both PRODUCT:UPDATE AND STOCK:UPDATE permissions
    await _productService.BulkUpdateAsync(dto);
    return Ok(Response<bool>.Success(true, "Bulk update completed"));
}
```

---

## Logical Operators

### OR Operator

```csharp
[AsmAuthorization(new[]
{
    new PermissionRequirement(ModuleCode.Admin, AccessType.Access),
    new PermissionRequirement(ModuleCode.Customer, AccessType.View)
}, LogicalOperator.OR)]
```

- ✅ Passes if user has `ADMIN:ACCESS` permission
- ✅ Passes if user has `CUSTOMER:VIEW` permission
- ❌ Fails if user has neither permission

### AND Operator

```csharp
[AsmAuthorization(new[]
{
    new PermissionRequirement(ModuleCode.Order, AccessType.Create),
    new PermissionRequirement(ModuleCode.Customer, AccessType.View)
}, LogicalOperator.AND)]
```

- ✅ Passes if user has both `ORDER:CREATE` AND `CUSTOMER:VIEW` permissions
- ❌ Fails if user has only one permission
- ❌ Fails if user has neither permission

---

## Permission Requirements

### Module Codes

Available module codes for the WebShop application:

| Module Code | Description | Common Permissions |
|-------------|-------------|-------------------|
| `Customer` | Customer management | View, Create, Update, Delete |
| `Product` | Product catalog | View, Create, Update, Delete |
| `Order` | Order processing | View, Create, Update, Delete |
| `Article` | Article management | View, Create, Update, Delete |
| `Stock` | Inventory management | View, Update |
| `Address` | Address management | View, Create, Update, Delete |
| `Size` | Size reference data | View, Create, Update, Delete |
| `Color` | Color reference data | View, Create, Update, Delete |
| `Label` | Label reference data | View, Create, Update, Delete |

### Access Types

| Access Type | Description |
|-------------|-------------|
| `View` | Read/list operations |
| `Create` | Create new resources |
| `Update` | Modify existing resources |
| `Delete` | Delete resources |
| `Access` | General access to module or section |
| `AllowAny` | Bypass all permission checks |

---

## Usage Examples

### E-commerce Management Operations

```csharp
[ApiController]
[Route("api/v1/management")]
public class ManagementController : BaseApiController
{
    // Management access requires access to multiple modules OR specific management permission
    [HttpGet("overview")]
    [AsmAuthorization(new[]
    {
        new PermissionRequirement(ModuleCode.Customer, AccessType.View),
        new PermissionRequirement(ModuleCode.Order, AccessType.View),
        new PermissionRequirement(ModuleCode.Product, AccessType.View)
    }, LogicalOperator.OR)]
    public async Task<IActionResult> GetOverview()
    {
        // User needs view access to at least one major module
        var overview = await _managementService.GetSystemOverviewAsync();
        return Ok(Response<OverviewDto>.Success(overview));
    }

    // Require both customer management and order processing permissions
    [HttpPost("customer-orders")]
    [AsmAuthorization(new[]
    {
        new PermissionRequirement(ModuleCode.Customer, AccessType.Update),
        new PermissionRequirement(ModuleCode.Order, AccessType.Update)
    }, LogicalOperator.AND)]
    public async Task<IActionResult> ProcessCustomerOrders()
    {
        // Must have both permissions for complex operations
        await _orderService.ProcessPendingOrdersAsync();
        return Ok(Response<bool>.Success(true, "Orders processed"));
    }
}
```

### Product Management

```csharp
[ApiController]
[Route("api/v1/products")]
public class ProductController : BaseApiController
{
    [HttpGet]
    [AsmAuthorization(ModuleCode.Product, AccessType.View)]
    public async Task<IActionResult> GetAll()
    {
        var products = await _productService.GetAllAsync();
        return Ok(Response<IReadOnlyList<ProductDto>>.Success(products));
    }

    [HttpPost]
    [AsmAuthorization(ModuleCode.Product, AccessType.Create)]
    public async Task<IActionResult> Create(CreateProductDto dto)
    {
        var product = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = product.Id },
            Response<ProductDto>.Success(product));
    }

    // Require both product update AND stock management permissions
    [HttpPut("{id}/stock")]
    [AsmAuthorization(new[]
    {
        new PermissionRequirement(ModuleCode.Product, AccessType.Update),
        new PermissionRequirement(ModuleCode.Stock, AccessType.Update)
    }, LogicalOperator.AND)]
    public async Task<IActionResult> UpdateStock(int id, UpdateStockDto dto)
    {
        await _productService.UpdateStockAsync(id, dto);
        return Ok(Response<bool>.Success(true, "Stock updated"));
    }
}
```

## Configuration

### Enable ASM Authorization

Add to `appsettings.json`:

```json
{
  "AppSettings": {
    "EnableAsmAuthorization": true
  }
}
```

**Note:** The ASM authorization services are always registered, but the authorization check is performed at runtime based on this configuration setting. When `EnableAsmAuthorization` is `false`, the authorization attributes will not perform any permission checks and all requests will be allowed through.

### ASM Service Configuration

Configure the ASM service connection:

```json
{
  "AsmService": {
    "Url": "https://api.uat.bapsapps.org/asm/api/v2/",
    "TimeoutSeconds": 30,
    "Headers": {
      "AuthAppId": "your-app-id",
      "AuthAppSecret": "your-app-secret"
    },
    "Endpoint": {
      "ApplicationSecurity": "application-security"
    }
  }
}
```

---

## Error Responses

### 401 Unauthorized

```json
{
  "succeeded": false,
  "data": null,
  "message": "Authentication required",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440000",
      "statusCode": 401,
      "message": "Authentication required"
    }
  ]
}
```

### 403 Forbidden - Insufficient Permissions

```json
{
  "succeeded": false,
  "data": null,
  "message": "Insufficient permissions for this operation",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440001",
      "statusCode": 403,
      "message": "Insufficient permissions for this operation"
    }
  ]
}
```

### 500 Internal Server Error - ASM Service Unavailable

```json
{
  "succeeded": false,
  "data": null,
  "message": "Authorization service temporarily unavailable",
  "errors": [
    {
      "errorId": "550e8400-e29b-41d4-a716-446655440002",
      "statusCode": 500,
      "message": "Authorization service temporarily unavailable"
    }
  ]
}
```

---

## Best Practices

### 1. Use Appropriate Granularity

```csharp
// ✅ Good - Specific permissions for sensitive operations
[AsmAuthorization(ModuleCode.Customer, AccessType.Update)]

// ❌ Avoid - Too broad permissions
[AsmAuthorization(ModuleCode.Customer, AccessType.AllowAny)]
```

### 2. Combine Permissions Logically

```csharp
// ✅ Good - OR for flexible access
[AsmAuthorization(new[]
{
    new PermissionRequirement(ModuleCode.Admin, AccessType.Access),
    new PermissionRequirement(ModuleCode.Customer, AccessType.View)
}, LogicalOperator.OR)]

// ✅ Good - AND for complex operations requiring multiple skills
[AsmAuthorization(new[]
{
    new PermissionRequirement(ModuleCode.Order, AccessType.Update),
    new PermissionRequirement(ModuleCode.Stock, AccessType.Update)
}, LogicalOperator.AND)]
```

### 3. Group Related Permissions

```csharp
// ✅ Good - Group related operations
private static readonly PermissionRequirement[] ReportPermissions = new[]
{
    new PermissionRequirement(ModuleCode.Customer, AccessType.View),
    new PermissionRequirement(ModuleCode.Order, AccessType.View),
    new PermissionRequirement(ModuleCode.Product, AccessType.View)
};

[AsmAuthorization(ReportPermissions, LogicalOperator.OR)]
```

### 4. Document Permission Requirements

```csharp
/// <summary>
/// Generates comprehensive sales reports.
/// Requires VIEW access to Customer, Order, or Product modules.
/// </summary>
[AsmAuthorization(new[]
{
    new PermissionRequirement(ModuleCode.Customer, AccessType.View),
    new PermissionRequirement(ModuleCode.Order, AccessType.View),
    new PermissionRequirement(ModuleCode.Product, AccessType.View)
}, LogicalOperator.OR)]
public async Task<IActionResult> GenerateSalesReport() { ... }
```

---

## Testing ASM Authorization

### Unit Tests

```csharp
[Fact]
public async Task GetCustomer_WithValidPermission_ReturnsCustomer()
{
    // Arrange
    var mockAsmService = new Mock<IAsmService>();
    mockAsmService.Setup(s => s.GetApplicationSecurityAsync(It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(new List<AsmResponseDto>
        {
            new AsmResponseDto
            {
                HasAccess = true,
                Permissions = new List<string> { "CUST:VIEW" }
            }
        });

    // Act & Assert
    // Test passes with CUST:VIEW permission
}
```

### Integration Tests

```csharp
[Fact]
public async Task CreateOrder_WithInsufficientPermissions_Returns403()
{
    // Arrange - User has VIEW but not CREATE permission
    // Act - Call POST /api/v1/orders
    // Assert - Returns 403 Forbidden
}
```

---

## Migration from Basic Authorization

If migrating from simple role-based authorization:

1. **Identify Permission Requirements**: Map existing roles to ASM permissions
2. **Update Controllers**: Replace `[Authorize]` with `[AsmAuthorization]`
3. **Test Thoroughly**: Verify permission logic works as expected
4. **Update Documentation**: Document new permission requirements

### Example Migration

```csharp
// Before
[Authorize(Roles = "Manager")]
public async Task<IActionResult> DeleteCustomer(int id) { ... }

// After
[AsmAuthorization(ModuleCode.Customer, AccessType.Delete)]
public async Task<IActionResult> DeleteCustomer(int id) { ... }
```

---

## Troubleshooting

### Common Issues

**"No permissions assigned to user"**

- Check ASM service configuration
- Verify user has active roles in ASM system
- Check network connectivity to ASM service

**"Authorization service temporarily unavailable"**

- Check ASM service health
- Verify service URL and credentials
- Check network connectivity

**Unexpected 403 responses**

- Verify user has required permissions in ASM
- Check logical operator usage (OR vs AND)
- Review permission requirement combinations

### Debug Mode

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "WebShop.Api.Filters.AsmAuthorizationValidation": "Debug",
      "WebShop.Api.Filters.Validators.AsmPermissionValidator": "Debug",
      "WebShop.Api.Filters.Factories.AsmErrorResponseFactory": "Debug"
    }
  }
}
```

### Architecture-Specific Issues

**"Could not resolve IAsmPermissionValidator"**
- Verify `ConfigureFilterServices()` is called in `Program.cs`
- Check that the services are registered in the DI container
- Ensure the filter is properly instantiated with all required dependencies

**"ASM authorization not working as expected"**
- Check that `EnableAsmAuthorization` is set to `true` in `appsettings.json`
- Verify the configuration key path is correct (`EnableAsmAuthorization` at root level)
- Check application logs for configuration loading issues

**"Permission validation failed unexpectedly"**
- Check `IAsmPermissionValidator` implementation logic
- Verify ASM service response format matches expectations
- Review permission string patterns (e.g., "CUST:VIEW")

**"Error response format incorrect"**
- Check `IAsmErrorResponseFactory` implementation
- Verify `Response<T>` structure is being used correctly
- Ensure proper HTTP status codes are returned

### Permission Matrix

Create a permission matrix for your application:

| Role | Customer | Product | Order |
|------|----------|---------|-------|
| Viewer | View | View | View |
| Editor | View/Create/Update | View/Create/Update | View/Create/Update |
| Manager | All | All | All |

This helps ensure consistent permission assignment across the application.
