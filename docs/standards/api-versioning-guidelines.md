# API Versioning Guidelines

[← Back to README](../../README.md)

## Table of Contents

1. [Overview](#overview)
2. [Versioning Strategy](#versioning-strategy)
3. [Implementation Details](#implementation-details)
4. [Version Specification Methods](#version-specification-methods)
5. [Controller Configuration](#controller-configuration)
6. [Deprecation Strategy](#deprecation-strategy)
7. [Migration Guidelines](#migration-guidelines)
8. [Best Practices](#best-practices)
9. [Examples](#examples)

---

## Overview

This document outlines the API versioning strategy, implementation, and best practices for the WebShop API. The API uses **major version only** (e.g., v1, v2, v3) following semantic versioning principles for breaking changes.

## Versioning Strategy

### Major Version Only

- **MUST** use major version only (e.g., `1`, `2`, `3`) - **NOT** major.minor (e.g., `1.0`, `2.1`)
- Major version increments indicate **breaking changes**:
  - Removed endpoints
  - Changed request/response schemas
  - Changed behavior that breaks backward compatibility
  - Removed or renamed required fields

### When to Create a New Version

Create a new major version when:
- ✅ Removing an endpoint
- ✅ Changing request/response structure (breaking changes)
- ✅ Changing required fields or validation rules
- ✅ Changing authentication/authorization behavior
- ✅ Changing business logic that affects client expectations

**DO NOT** create a new version for:
- ❌ Adding new endpoints (add to existing version)
- ❌ Adding optional fields (backward compatible)
- ❌ Bug fixes (fix in existing version)
- ❌ Performance improvements (fix in existing version)
- ❌ Adding new optional query parameters

### Version Lifecycle

1. **Active**: Current version, fully supported
2. **Deprecated**: Still functional but will be removed (with deprecation headers)
3. **Sunset**: Removed and no longer available

### Backward Compatibility Policy

**MUST maintain backward compatibility for 2 versions back:**

- If current version is **v3**, you **MUST** support:
  - ✅ **v3** (current)
  - ✅ **v2** (previous)
  - ✅ **v1** (2 versions back)

- When **v4** is released:
  - ✅ **v4** (current)
  - ✅ **v3** (previous)
  - ✅ **v2** (2 versions back)
  - ⚠️ **v1** can be deprecated/sunset (no longer required)

**Rationale:**
- Provides sufficient time for clients to migrate
- Reduces breaking changes impact
- Allows gradual migration path
- Industry best practice (e.g., Microsoft, Google APIs)

**Example Timeline:**
- **January 2025**: v3 released → Support v1, v2, v3
- **July 2025**: v4 released → Support v2, v3, v4 (v1 can be deprecated)
- **January 2026**: v5 released → Support v3, v4, v5 (v2 can be deprecated)

---

## Implementation Details

### Configuration

API versioning is configured in `ApiVersioningExtensions.cs`:

```csharp
services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("api-version")
    );
});
```

### Key Settings

- **`DefaultApiVersion`**: Default version when none specified (currently `1`)
- **`AssumeDefaultVersionWhenUnspecified`**: If `true`, uses default version when not specified
- **`ReportApiVersions`**: Adds `api-supported-versions` and `api-deprecated-versions` headers to responses
- **`ApiVersionReader`**: Two ways to specify version:
  - **URL Segment** (primary method): `/api/v{version}/[controller]`
  - **HTTP Header**: `api-version: {version}`

### Supported Version Specification Methods

**Only two methods are supported:**

1. ✅ **URL Segment** (Primary) - `/api/v1/products`
2. ✅ **HTTP Header** - `api-version: 1`

**Removed methods (no longer supported):**
- ❌ Query String Parameter (`?version=1`)
- ❌ Media Type Parameter (`Accept: application/json; ver=1`)

---

## Version Specification Methods

Clients can specify the API version using **two supported methods** (in order of precedence):

### 1. URL Segment (Primary Method) ✅ **RECOMMENDED**

**Format:** `/api/v{version}/[controller]`

**Example:**
```
GET /api/v1/products
GET /api/v2/products
```

**Advantages:**
- Most explicit and visible
- Easy to understand and document
- RESTful and resource-oriented
- Cacheable URLs
- Works with all HTTP clients and tools

**Precedence:** Highest (checked first)

### 2. HTTP Header (Alternative Method)

**Header Name:** `api-version`

**Example:**
```http
GET /api/products
api-version: 1
```

**Use Case:** When you want to keep URLs clean but still specify version programmatically

**Precedence:** Lower (checked after URL segment)

**Important Notes:**
- Header name is **`api-version`** (lowercase, hyphenated) - **NOT** `X-Version`
- Must be included in CORS `AllowedHeaders` if using cross-origin requests
- Useful for API clients that want version-agnostic URLs

### Removed Methods ❌

The following methods are **no longer supported**:

- ❌ **Query String Parameter** (`?version=1`) - Removed for security and consistency
- ❌ **Media Type Parameter** (`Accept: application/json; ver=1`) - Removed for simplicity

**Rationale:**
- Simplifies API surface
- Reduces confusion about which method to use
- URL segment is most explicit and RESTful
- HTTP header provides flexibility when needed
- Query strings can be logged/exposed in URLs
- Media type versioning is less common and harder to discover

---

## Controller Configuration

### Basic Controller Setup

```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class ProductsController : BaseApiController
{
    // Controller implementation
}
```

### Multiple Versions in Same Controller

```csharp
[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
public class ProductsController : BaseApiController
{
    [HttpGet]
    [MapToApiVersion("1")]
    public async Task<ActionResult<Response<IReadOnlyList<ProductDto>>>> GetAllV1()
    {
        // Version 1 implementation
    }

    [HttpGet]
    [MapToApiVersion("2")]
    public async Task<ActionResult<Response<IReadOnlyList<ProductDtoV2>>>> GetAllV2()
    {
        // Version 2 implementation
    }
}
```

### Version-Specific Endpoints

```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : BaseApiController
{
    // Available in v1 only
    [HttpGet("legacy")]
    public async Task<ActionResult> GetLegacy() { }

    // Available in v1 and v2
    [HttpGet]
    [MapToApiVersion("1")]
    [MapToApiVersion("2")]
    public async Task<ActionResult> GetAll() { }
}
```

### Important Notes

- **MUST** use `[ApiVersion("1")]` (not `[ApiVersion("1.0")]`)
- **MUST** use route template `api/v{version:apiVersion}/[controller]`
- **SHOULD** use `[MapToApiVersion]` when an endpoint is available in multiple versions

---

## Deprecation Strategy

### Configuration-Based Deprecation

Deprecation is configured in `appsettings.json`:

```json
{
  "ApiVersionDeprecationOptions": {
    "DeprecatedVersions": [
      {
        "MajorVersion": 1,
        "IsDeprecated": true,
        "SunsetDate": "Mon, 01 Jan 2026 00:00:00 GMT",
        "SuccessorVersionUrl": "/api/v2",
        "DeprecationMessage": "true"
      }
    ]
  }
}
```

### Deprecation Headers

When a version is deprecated, the following headers are automatically added to responses:

#### 1. Deprecation Header (RFC 8594)

**Header:** `Deprecation: true` or `Deprecation: <date-time>`

**Example:**
```http
HTTP/1.1 200 OK
Deprecation: true
```

**Purpose:** Indicates that the API version is deprecated

#### 2. Sunset Header (RFC 8595)

**Header:** `Sunset: <date-time>`

**Example:**
```http
HTTP/1.1 200 OK
Sunset: Mon, 01 Jan 2026 00:00:00 GMT
```

**Purpose:** Specifies when the deprecated version will be removed

**Format:** RFC 7231 date-time format (e.g., "Mon, 01 Jan 2026 00:00:00 GMT")

#### 3. Link Header (RFC 8288)

**Header:** `Link: <successor-url>; rel="successor-version"`

**Example:**
```http
HTTP/1.1 200 OK
Link: </api/v2>; rel="successor-version"
```

**Purpose:** Points to the successor version for migration

### Deprecation Workflow

**Important:** Deprecation must follow the **2 versions back** backward compatibility policy:

1. **Announce Deprecation** (3-6 months before sunset):
   - Set `IsDeprecated: true` in configuration
   - Set `SunsetDate` to removal date (minimum 3-6 months notice)
   - Set `SuccessorVersionUrl` to new version
   - Update API documentation
   - Notify API consumers via email, documentation, and deprecation headers

2. **Monitor Usage**:
   - Track usage of deprecated version
   - Identify consumers still using deprecated version
   - Provide migration support and guidance
   - Monitor migration progress

3. **Sunset Date** (only after 2 versions back policy allows):
   - **MUST** maintain support for 2 versions back
   - Can only sunset versions beyond 2 versions back
   - Example: If current is v3, can sunset v1 (v2 and v3 must remain)
   - Remove deprecated version endpoints or return 410 Gone
   - Update documentation

**Example Deprecation Timeline:**
- **Current**: v3 (support v1, v2, v3)
- **Deprecate v1**: Set deprecation headers, announce removal in 6 months
- **Release v4**: Support v2, v3, v4 (v1 can now be sunset)
- **Sunset v1**: Remove v1 endpoints, return 410 Gone

### Example: Deprecating v1

**Step 1: Announce Deprecation (January 2025)**

```json
{
  "ApiVersionDeprecationOptions": {
    "DeprecatedVersions": [
      {
        "MajorVersion": 1,
        "IsDeprecated": true,
        "SunsetDate": "Mon, 01 Jul 2025 00:00:00 GMT",
        "SuccessorVersionUrl": "/api/v2",
        "DeprecationMessage": "true"
      }
    ]
  }
}
```

**Step 2: After Sunset Date (July 2025)**

Remove v1 endpoints or return 410 Gone:

```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ProductsController : BaseApiController
{
    [HttpGet]
    public ActionResult Deprecated()
    {
        return StatusCode(410, new { 
            message = "API version 1 has been removed. Please migrate to v2.",
            successorUrl = "/api/v2"
        });
    }
}
```

---

## Migration Guidelines

### For API Consumers

1. **Monitor Deprecation Headers**:
   - Check `Deprecation` header in responses
   - Note `Sunset` date
   - Follow `Link` header to successor version

2. **Plan Migration**:
   - Review breaking changes in new version
   - Update client code
   - Test against new version
   - Deploy before sunset date

3. **Handle Breaking Changes**:
   - Update request/response models
   - Update endpoint URLs if changed
   - Update authentication if changed
   - Update business logic if behavior changed

### For API Developers

1. **Document Breaking Changes**:
   - List all removed endpoints
   - List all changed endpoints
   - Provide migration examples
   - Update OpenAPI/Swagger documentation

2. **Provide Migration Tools**:
   - Code examples for new version
   - Migration guides
   - Side-by-side comparison

3. **Support Period**:
   - **MUST** maintain support for 2 versions back (backward compatibility policy)
   - Maintain deprecated version for minimum 3-6 months before sunset
   - Provide support during migration
   - Monitor usage and assist with migration
   - Only sunset versions beyond 2 versions back

---

## Best Practices

### Versioning

1. **✅ DO:**
   - Use major version only (1, 2, 3)
   - Include version in URL path (primary method)
   - Use `api-version` header as alternative method
   - Document breaking changes clearly
   - Provide migration guides
   - Give sufficient notice before deprecation (3-6 months)
   - **Maintain backward compatibility for 2 versions back**

2. **❌ DON'T:**
   - Use minor versions (1.0, 1.1, 2.0)
   - Create new version for non-breaking changes
   - Remove versions without deprecation notice
   - Change version behavior without documentation
   - Use query string or media type versioning (removed)
   - Remove versions that are within 2 versions back

### Deprecation

1. **✅ DO:**
   - Announce deprecation 3-6 months in advance
   - Set clear sunset date
   - Provide successor version URL
   - Monitor usage of deprecated versions
   - Support consumers during migration
   - **Respect 2 versions back backward compatibility policy**
   - Only sunset versions beyond 2 versions back

2. **❌ DON'T:**
   - Deprecate without notice
   - Remove versions abruptly
   - Deprecate without successor version
   - Ignore consumer feedback
   - Sunset versions that are within 2 versions back

### Controller Implementation

1. **✅ DO:**
   - Use `[ApiVersion("1")]` attribute
   - Use route template `api/v{version:apiVersion}/[controller]`
   - Use `[MapToApiVersion]` for multi-version endpoints
   - Inherit from `BaseApiController`
   - Document version-specific behavior

2. **❌ DON'T:**
   - Use `[ApiVersion("1.0")]`
   - Hardcode version in route
   - Mix versioning strategies
   - Skip version attributes

---

## Examples

### Example 1: Basic Versioned Controller

```csharp
using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services;

namespace WebShop.Api.Controllers;

/// <summary>
/// Manages product resources.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/products")]
[Produces("application/json")]
public class ProductController(
    IProductService productService,
    ILogger<ProductController> logger) : BaseApiController
{
    private readonly IProductService _productService = productService;
    private readonly ILogger<ProductController> _logger = logger;

    /// <summary>
    /// Gets all products.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<ProductDto>>>> GetAll(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductDto> products = await _productService.GetAllAsync(cancellationToken);
        return Ok(Response<IReadOnlyList<ProductDto>>.Success(products, "Products retrieved successfully"));
    }
}
```

### Example 2: Multi-Version Controller

```csharp
[ApiController]
[ApiVersion("1")]
[ApiVersion("2")]
[Route("api/v{version:apiVersion}/products")]
public class ProductController : BaseApiController
{
    // Version 1: Returns simple product list
    [HttpGet]
    [MapToApiVersion("1")]
    public async Task<ActionResult<Response<IReadOnlyList<ProductDto>>>> GetAllV1()
    {
        // v1 implementation
    }

    // Version 2: Returns paginated product list
    [HttpGet]
    [MapToApiVersion("2")]
    public async Task<ActionResult<Response<PagedResult<ProductDtoV2>>>> GetAllV2(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        // v2 implementation with pagination
    }
}
```

### Example 3: Version-Specific Endpoint

```csharp
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/products")]
public class ProductController : BaseApiController
{
    // Available in v1 only (legacy endpoint)
    [HttpGet("legacy")]
    public async Task<ActionResult> GetLegacyFormat()
    {
        // Legacy implementation
    }

    // Available in both v1 and v2
    [HttpGet("{id}")]
    [MapToApiVersion("1")]
    [MapToApiVersion("2")]
    public async Task<ActionResult<Response<ProductDto>>> GetById(int id)
    {
        // Shared implementation
    }
}
```

### Example 4: Deprecation Configuration

```json
{
  "ApiVersionDeprecationOptions": {
    "DeprecatedVersions": [
      {
        "MajorVersion": 1,
        "IsDeprecated": true,
        "SunsetDate": "Mon, 01 Jul 2025 00:00:00 GMT",
        "SuccessorVersionUrl": "/api/v2",
        "DeprecationMessage": "true"
      }
    ]
  }
}
```

**Response Headers (when calling v1):**
```http
HTTP/1.1 200 OK
Deprecation: true
Sunset: Mon, 01 Jul 2025 00:00:00 GMT
Link: </api/v2>; rel="successor-version"
api-supported-versions: 1, 2
api-deprecated-versions: 1
```

---

## Testing

### Testing Version Selection

```csharp
[Fact]
public async Task GetProducts_WithUrlVersion_ReturnsV1()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/v1/products");
    
    // Assert
    response.EnsureSuccessStatusCode();
    // Verify v1 response format
}

[Fact]
public async Task GetProducts_WithHeaderVersion_ReturnsV1()
{
    // Arrange
    var client = _factory.CreateClient();
    client.DefaultRequestHeaders.Add("api-version", "1");
    
    // Act
    var response = await client.GetAsync("/api/products");
    
    // Assert
    response.EnsureSuccessStatusCode();
    // Verify v1 response format
}
```

### Testing Deprecation Headers

```csharp
[Fact]
public async Task GetProducts_V1Deprecated_ReturnsDeprecationHeaders()
{
    // Arrange
    var client = _factory.CreateClient();
    
    // Act
    var response = await client.GetAsync("/api/v1/products");
    
    // Assert
    Assert.True(response.Headers.Contains("Deprecation"));
    Assert.True(response.Headers.Contains("Sunset"));
    Assert.True(response.Headers.Contains("Link"));
    
    var linkHeader = response.Headers.GetValues("Link").First();
    Assert.Contains("successor-version", linkHeader);
}
```

---

## Summary

- **Version Format**: Major version only (1, 2, 3)
- **URL Pattern**: `/api/v{version}/[controller]` (primary method)
- **Header Method**: `api-version` HTTP header (alternative method)
- **Removed Methods**: Query string and media type versioning (no longer supported)
- **Backward Compatibility**: **MUST** maintain support for 2 versions back
- **Deprecation**: Configuration-based with standard HTTP headers (RFC 8594, RFC 8595, RFC 8288)
- **Migration**: 3-6 months notice with migration support
- **Best Practice**: Document breaking changes, provide migration guides, monitor usage

### Key Requirements

1. ✅ **Two Version Methods Only**:
   - URL Segment: `/api/v1/products` (primary, recommended)
   - HTTP Header: `api-version: 1` (alternative)

2. ✅ **Backward Compatibility Policy**:
   - Always support current version + 2 previous versions
   - Example: If v3 is current, support v1, v2, v3
   - Can only sunset versions beyond 2 versions back

3. ✅ **Deprecation Strategy**:
   - Configuration-based deprecation headers
   - Minimum 3-6 months notice before sunset
   - Clear migration path with successor version

For questions or issues, refer to the API documentation or contact the development team.

