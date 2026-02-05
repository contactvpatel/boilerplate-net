# Naming Guidelines

This document defines the naming conventions and guidelines for the WebShop .NET API codebase. All developers **MUST** follow these guidelines to ensure consistency, readability, and maintainability across the project.

[← Back to README](../../README.md)

## Table of Contents

- [General Principles](#general-principles)
- [Quick Reference Table](#quick-reference-table)
- [Namespaces](#namespaces)
- [Types (Classes, Interfaces, Structs, Enums)](#types-classes-interfaces-structs-enums)
- [Methods](#methods)
- [Properties](#properties)
- [Fields](#fields)
- [Parameters](#parameters)
- [Local Variables](#local-variables)
- [Constants](#constants)
- [DTOs (Data Transfer Objects)](#dtos-data-transfer-objects)
- [Extension Methods](#extension-methods)
- [Generic Types](#generic-types)
- [File and Folder Names](#file-and-folder-names)
- [Special Cases](#special-cases)
- [Examples](#examples)
- [Common Mistakes to Avoid](#common-mistakes-to-avoid)

---

## General Principles

These guidelines are based on [Microsoft's official C# naming conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names).

1. **PascalCase** for all public members (classes, interfaces, methods, properties, etc.)
2. **camelCase** for private fields, parameters, and local variables
3. **Descriptive Names**: Use clear, descriptive names that explain intent
4. **Prefer Clarity Over Brevity**: Use meaningful names rather than short abbreviations
5. **Avoid Abbreviations**: Use full words unless the abbreviation is industry-standard (e.g., `Id`, `Dto`, `Api`, `Http`, `Ssl`, `Url`)
6. **No Consecutive Underscores**: Identifiers **MUST NOT** contain two consecutive underscore (`__`) characters (reserved for compiler-generated identifiers)
7. **Consistency**: Follow established patterns throughout the codebase
8. **Microsoft Conventions**: Align with Microsoft .NET naming conventions and guidelines

---

## Quick Reference Table

| Category | Element | Casing | Distinctive Rules | Example |
|----------|---------|--------|-------------------|---------|
| **Types** | Class / Struct | PascalCase | Noun; Match file name | `ProductService` |
| | Interface | PascalCase | Prefix with `I` | `IProductRepository` |
| | Enum | PascalCase | Singular (standard), Plural (flags) | `LogLevel`, `FilePermissions` |
| | DTO | PascalCase | Suffix with `Dto` | `ProductDto` |
| **Members** | Method | PascalCase | Verb; Async methods end in `Async` | `GetByIdAsync` |
| | Property | PascalCase | Boolean properties use `Is`, `Has`, `Can` | `IsActive`, `Name` |
| | Constant | PascalCase | - | `DefaultTimeout` |
| **Fields** | Private / Protected | camelCase | Prefix with `_` | `_logger` |
| | Static Readonly (Immutable) | PascalCase | For collections, regex, etc. | `ValidationPattern` |
| | Static Mutable | camelCase | Prefix with `_` | `_cache` |
| **Variables** | Parameter | camelCase | - | `cancellationToken` |
| | Local Variable | camelCase | - | `productCount` |
| **Constructors** | Class / Struct Primary | camelCase | Matches parameters | `logger` |
| | Record Primary | PascalCase | Matches properties | `FirstName` |
| **Organization** | Namespace | PascalCase | `WebShop.` + Folder Structure | `WebShop.Core.Entities` |
| | File Name | PascalCase | Match type name | `ProductService.cs` |

---

## Namespaces

### Namespace Rules

- **MUST** use PascalCase
- **MUST** match folder structure
- **MUST** start with `WebShop.` followed by project name
- **MUST** use sub-namespaces for organization (e.g., `Base`, `Services`, `Features`)

### Namespace Examples

```csharp
// ✅ CORRECT
namespace WebShop.Core.Entities;
namespace WebShop.Core.Interfaces.Base;
namespace WebShop.Core.Interfaces.Services;
namespace WebShop.Business.DTOs;
namespace WebShop.Business.Services;
namespace WebShop.Business.Services.Interfaces;
namespace WebShop.Infrastructure.Repositories;
namespace WebShop.Infrastructure.Repositories.Base;
namespace WebShop.Infrastructure.Services.External;
namespace WebShop.Infrastructure.Services.Internal;
namespace WebShop.Api.Controllers;
namespace WebShop.Api.Extensions.Core;
namespace WebShop.Api.Extensions.Features;
namespace WebShop.Api.Extensions.Middleware;
namespace WebShop.Api.Extensions.Utilities;
namespace WebShop.Util.Models;
namespace WebShop.Util.OpenTelemetry.Helpers;

// ❌ INCORRECT
namespace webshop.core.entities;  // Wrong case
namespace WebShop.Core.Entities.Base;  // Should be WebShop.Core.Interfaces.Base
namespace WebShop.Business.Dtos;  // Should be DTOs (industry standard)
```

### Namespace Organization

- **Core Layer**: `WebShop.Core.{Category}` (e.g., `Entities`, `Interfaces.Base`, `Interfaces.Services`, `Models`)
- **Business Layer**: `WebShop.Business.{Category}` (e.g., `DTOs`, `Services`, `Services.Interfaces`, `Mappings`, `Validators`)
- **Infrastructure Layer**: `WebShop.Infrastructure.{Category}` (e.g., `Repositories`, `Repositories.Base`, `Repositories.Helpers`, `Services.External`, `Services.Internal`, `Data`, `Helpers`)
- **API Layer**: `WebShop.Api.{Category}` (e.g., `Controllers`, `Filters`, `Middleware`, `Extensions.Core`, `Extensions.Features`, `Extensions.Middleware`, `Extensions.Utilities`, `Models`, `Helpers`)
- **Util Layer**: `WebShop.Util.{Category}` (e.g., `Models`, `OpenTelemetry.Configuration`, `OpenTelemetry.Extensions`, `OpenTelemetry.Helpers`, `Security`)

---

## Types (Classes, Interfaces, Structs, Enums)

### Classes

- **MUST** use PascalCase
- **MUST** be nouns or noun phrases
- **SHOULD** be singular (e.g., `Product`, `Customer`, not `Products`, `Customers`)
- **MUST** use descriptive names

```csharp
// ✅ CORRECT
public class Product { }
public class Customer { }
public class BaseEntity { }
public class Repository<T> { }
public class ProductService { }
public class ServiceExtensions { }
public class DbUpLoggerExtension { }

// ❌ INCORRECT
public class product { }  // Wrong case
public class Products { }  // Should be singular
public class ProductSvc { }  // Avoid abbreviations
public class ProductServiceClass { }  // Redundant suffix
```

### Interfaces

- **MUST** use PascalCase
- **MUST** start with `I` prefix
- **MUST** be nouns or noun phrases
- **SHOULD** be singular

```csharp
// ✅ CORRECT
public interface IRepository<T> { }
public interface IProductService { }
public interface IUserContext { }
public interface ICacheService { }
public interface ISsoService { }

// ❌ INCORRECT
public interface Repository<T> { }  // Missing I prefix
public interface IProductServices { }  // Should be singular
public interface IProductSvc { }  // Avoid abbreviations
```

### Abstract Classes

- **MUST** use PascalCase
- **MUST** be nouns or noun phrases
- **SHOULD** use `Base` prefix for base classes (e.g., `BaseEntity`, `BaseApiController`)

```csharp
// ✅ CORRECT
public abstract class BaseEntity { }
public abstract class BaseApiController : ControllerBase { }
public abstract class HttpServiceBase { }

// ❌ INCORRECT
public abstract class Entity { }  // Should indicate it's a base class
public abstract class Base { }  // Too generic
```

### Static Classes

- **MUST** use PascalCase
- **MUST** be nouns or noun phrases
- **SHOULD** use `Extensions` suffix for extension method classes

```csharp
// ✅ CORRECT
public static class ServiceExtensions { }
public static class ApiVersioningExtensions { }
public static class ConfigurationExtensions { }
public static class DbConnectionStringCache { }

// ❌ INCORRECT
public static class ServiceExtension { }  // Should be plural for extensions
public static class Extensions { }  // Too generic
```

### Enums

- **MUST** use PascalCase
- **MUST** use singular noun for non-flags enums
- **MUST** use plural noun for flags enums (enums decorated with `[Flags]`)
- Enum values **MUST** use PascalCase

```csharp
// ✅ CORRECT (Non-flags - singular noun)
public enum CompressionLevel
{
    Optimal,
    Fastest,
    NoCompression,
    SmallestSize
}

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}

// ✅ CORRECT (Flags - plural noun)
[Flags]
public enum FilePermissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Execute = 4
}

// ❌ INCORRECT
public enum compressionLevel { }  // Wrong case
public enum CompressionLevels { }  // Should be singular (unless flags)
public enum CompressionLevel
{
    optimal,  // Wrong case
    FASTEST  // Wrong case
}
```

---

## Methods

### General Rules

- **MUST** use PascalCase
- **MUST** be verbs or verb phrases
- **MUST** use `Async` suffix for async methods
- **SHOULD** be descriptive and indicate action

### Async Methods

- **MUST** return `Task` or `Task<T>`
- **MUST** end with `Async` suffix (except controller actions - see exception below)
- **MUST** accept `CancellationToken cancellationToken = default` as last parameter

```csharp
// ✅ CORRECT
public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
public async Task<ProductDto> CreateAsync(CreateProductDto createDto, CancellationToken cancellationToken = default)
public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)

// ❌ INCORRECT
public async Task<ProductDto?> GetById(int id)  // Missing Async suffix (unless controller action)
public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken)  // Should use full name
public Task<ProductDto?> GetByIdAsync(int id)  // Missing async keyword
```

#### Exception: Controller Actions

**Controller action methods** (methods decorated with HTTP verb attributes like `[HttpGet]`, `[HttpPost]`, etc.) **MAY** omit the `Async` suffix for cleaner and more RESTful route names. This is an accepted exception to the general async naming rule.

**Rationale:**

- Controller actions are already clearly async by returning `Task<ActionResult<T>>`
- Route names are cleaner without `Async` suffix (e.g., `/api/products/{id}` vs `/api/products/{id}/async`)
- This follows ASP.NET Core conventions and common REST API practices

```csharp
// ✅ CORRECT (Controller Actions - Async suffix optional)
[HttpGet("{id}")]
public async Task<ActionResult<Response<ProductDto>>> GetById(
    [FromRoute] int id,
    CancellationToken cancellationToken)
{
    ProductDto? product = await _productService.GetByIdAsync(id, cancellationToken);
    if (product == null)
    {
        return NotFoundResponse<ProductDto>("Product not found", $"Product with ID {id} not found");
    }
    return Ok(Response<ProductDto>.Success(product, "Product retrieved successfully"));
}

[HttpPost]
public async Task<ActionResult<Response<ProductDto>>> Create(
    [FromBody] CreateProductDto createDto,
    CancellationToken cancellationToken)
{
    ProductDto product = await _productService.CreateAsync(createDto, cancellationToken);
    return CreatedAtAction(nameof(GetById), new { id = product.Id }, Response<ProductDto>.Success(product, "Product created successfully"));
}

// ✅ ALSO CORRECT (Service/Repository Methods - Async suffix REQUIRED)
public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
public async Task<ProductDto> CreateAsync(CreateProductDto createDto, CancellationToken cancellationToken = default)
```

**Note:** While controller actions may omit the `Async` suffix, it's still acceptable to include it for consistency. The codebase uses both patterns, and both are considered correct.

### Synchronous Methods

- **MUST** use PascalCase
- **MUST NOT** use `Async` suffix
- **SHOULD** be verbs or verb phrases

```csharp
// ✅ CORRECT
public void ConfigureServices(IServiceCollection services)
public string GetUserId()
public bool ValidateConnection(Dapper connection context)

// ❌ INCORRECT
public void ConfigureServicesAsync(IServiceCollection services)  // Not async, shouldn't have Async suffix
public void configureServices(IServiceCollection services)  // Wrong case
```

### Extension Methods

- **MUST** use PascalCase
- **MUST** be in static classes with `Extensions` suffix
- **MUST** be static methods
- **SHOULD** use descriptive names

```csharp
// ✅ CORRECT
public static void ConfigureApiServices(this IServiceCollection services, IConfiguration configuration)
public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
public static void UseExceptionHandling(this IApplicationBuilder app, Action<ExceptionHandlingOptions> configureOptions)

// ❌ INCORRECT
public static void configureApiServices(this IServiceCollection services)  // Wrong case
public static void ConfigureApiServicesAsync(this IServiceCollection services)  // Not async
```

### Override Methods

- **MUST** use `override` keyword
- **MUST** use PascalCase
- **MUST** match base class/interface signature

```csharp
// ✅ CORRECT
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
protected override string HttpClientName => "SsoService";
protected override HttpClient CreateHttpClient()

// ❌ INCORRECT
public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)  // Missing override
public override async Task<int> saveChangesAsync(CancellationToken cancellationToken)  // Wrong case
```

---

## Properties

### Property Rules

- **MUST** use PascalCase
- **MUST** be nouns or noun phrases
- **SHOULD** be descriptive
- **SHOULD** use `Id` (not `ID`) for identifiers
- **SHOULD** use `Is`, `Has`, `Can` prefix for boolean properties

```csharp
// ✅ CORRECT
public int Id { get; set; }
public string? Name { get; set; }
public DateTime CreatedAt { get; set; }
public DateTime? UpdatedAt { get; set; }
public bool IsActive { get; set; }
public bool HasPermission { get; set; }
public bool CanEdit { get; set; }
public int? CustomerId { get; set; }

// ❌ INCORRECT
public int id { get; set; }  // Wrong case
public int ID { get; set; }  // Should be Id
public bool active { get; set; }  // Should be IsActive
public bool isActive { get; set; }  // Wrong case
public string? customerName { get; set; }  // Wrong case
```

### Computed Properties

- **MUST** use PascalCase
- **SHOULD** be read-only when possible

```csharp
// ✅ CORRECT
protected override string HttpClientName => "SsoService";
public string FullName => $"{FirstName} {LastName}";
public bool IsValid => !string.IsNullOrWhiteSpace(Name) && Id > 0;

// ❌ INCORRECT
protected override string httpClientName => "SsoService";  // Wrong case
public string fullName => $"{FirstName} {LastName}";  // Wrong case
```

---

## Fields

### Private Fields

- **MUST** use camelCase with underscore prefix (`_`)
- **SHOULD** use `readonly` when possible
- **SHOULD** be descriptive

```csharp
// ✅ CORRECT
private readonly IDapperConnectionFactory _readContext;
private readonly IDapperConnectionFactory _writeContext;
private readonly ILogger<ProductService> _logger;
private readonly IProductRepository _productRepository;
private const string UserIdKey = "UserId";

// ❌ INCORRECT
private readonly IDapperConnectionFactory readContext;  // Missing underscore
private readonly IDapperConnectionFactory ReadContext;  // Wrong case
private readonly ILogger<ProductService> logger;  // Missing underscore
```

### Protected Fields

- **MUST** use camelCase with underscore prefix (`_`)
- **SHOULD** use `readonly` when possible

```csharp
// ✅ CORRECT
protected readonly IDapperConnectionFactory _connectionFactory;
protected readonly IRepository<Customer> _repository;
protected readonly ILogger<CustomerService>? _logger;

// ❌ INCORRECT
protected readonly IDapperConnectionFactory connectionFactory;  // Missing underscore
protected readonly IDapperConnectionFactory ConnectionFactory;  // Wrong case
```

### Static Fields

Static fields follow different naming conventions based on their mutability and purpose:

#### Static Readonly Fields (Immutable/Effectively Constants)

- **MUST** use **PascalCase** (same as constants)
- **MUST** be `readonly`
- Used for immutable collections, dictionaries, arrays, and compiled Regex patterns that are initialized once

```csharp
// ✅ CORRECT (Immutable static readonly - PascalCase)
public static class TagNameMapper
{
    private static readonly FrozenDictionary<string, string> TagNameMap = new(...);
    private static readonly string[] ConvertiblePrefixes = new[] { ... };
}

public static class SensitiveDataMaskingHelper
{
    private static readonly Regex QueryStringPattern = new(...);
    private static readonly Regex PasswordPattern = new(...);
}

public class OpenTelemetryEnricher
{
    private static readonly HashSet<string> ExcludedHeaders = new(...);
    private static readonly HashSet<string> ExcludedTags = new(...);
}
```

#### Static Mutable Fields

- **MUST** use **camelCase with underscore prefix** (`_`) (same as instance fields)
- Used for mutable static fields like `ConcurrentDictionary`, caches, etc.

```csharp
// ✅ CORRECT (Mutable static - underscore prefix)
internal static class DbConnectionStringCache
{
    private static readonly ConcurrentDictionary<string, string> _cache = new();
}
```

**Summary:**

- **Static readonly immutable** (collections, dictionaries, arrays, Regex) → **PascalCase**
- **Static mutable** (caches, dictionaries that change) → **camelCase with `_` prefix**

```csharp
// ❌ INCORRECT
private static readonly Dictionary<string, string> tagNameMap = new(...);  // Should be PascalCase
private static readonly ConcurrentDictionary<string, string> Cache = new();  // Should be _cache
private static readonly Regex queryStringPattern = new(...);  // Should be PascalCase
```

### Public Fields

- **SHOULD** be avoided (use properties instead)
- If used, **MUST** use PascalCase

```csharp
// ✅ CORRECT (but prefer properties)
public const string DefaultPolicy = "Default";

// ❌ INCORRECT
public const string defaultPolicy = "Default";  // Wrong case
```

---

## Parameters

### Rules

- **MUST** use camelCase
- **MUST** be descriptive
- **MUST** use `cancellationToken` (not `ct`, `token`, `cancelToken`) for cancellation tokens
- **SHOULD** use `dto` suffix for DTO parameters (e.g., `createDto`, `updateDto`)

```csharp
// ✅ CORRECT
public async Task<ProductDto> CreateAsync(CreateProductDto createDto, CancellationToken cancellationToken = default)
public async Task<ProductDto?> UpdateAsync(int id, UpdateProductDto updateDto, CancellationToken cancellationToken = default)
public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
public string GetUserId(HttpContext context)

// ❌ INCORRECT
public async Task<ProductDto> CreateAsync(CreateProductDto CreateDto, CancellationToken cancellationToken = default)  // Wrong case, wrong parameter name
public async Task<ProductDto> CreateAsync(CreateProductDto dto, CancellationToken token = default)  // Not descriptive, wrong parameter name
public void ConfigureServices(IServiceCollection Services, IConfiguration Configuration)  // Wrong case
```

### Parameter Ordering

- **MUST** follow this order:
  1. Required parameters
  2. Optional parameters
  3. `CancellationToken cancellationToken = default` (always last)

```csharp
// ✅ CORRECT
public async Task<ProductDto> CreateAsync(
    CreateProductDto createDto,
    CancellationToken cancellationToken = default)

public async Task<ProductDto?> UpdateAsync(
    int id,
    UpdateProductDto updateDto,
    CancellationToken cancellationToken = default)

// ❌ INCORRECT
public async Task<ProductDto> CreateAsync(
    CancellationToken cancellationToken = default,  // Should be last
    CreateProductDto createDto)
```

### Primary Constructor Parameters

Primary constructor parameters follow different naming conventions depending on the type being declared, as per [Microsoft's guidelines](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names):

- **For `class` and `struct` types**: Use camelCase (consistent with other method parameters)
- **For `record` types**: Use PascalCase (as the parameters become public properties)

```csharp
// ✅ CORRECT (Class - camelCase)
public class DataService(IWorkerQueue workerQueue, ILogger logger)
{
    public void ProcessData()
    {
        logger.LogInformation("Processing data");
        workerQueue.Enqueue("data");
    }
}

// ✅ CORRECT (Struct - camelCase)
public struct Point(double x, double y)
{
    public double Distance => Math.Sqrt(x * x + y * y);
}

// ✅ CORRECT (Record - PascalCase)
public record Person(string FirstName, string LastName);
public record Address(string Street, string City, string PostalCode);

// ❌ INCORRECT
public class DataService(IWorkerQueue WorkerQueue, ILogger Logger)  // Should be camelCase
public record Person(string firstName, string lastName);  // Should be PascalCase for records
```

---

## Local Variables

### Variable Rules

- **MUST** use camelCase
- **MUST** be descriptive
- **SHOULD** use `var` when type is obvious from right-hand side

```csharp
// ✅ CORRECT
var products = await _productRepository.GetAllAsync(cancellationToken);
List<ProductDto> productDtos = products.Adapt<List<ProductDto>>();
int count = products.Count;
string? name = product?.Name;
var logger = loggerFactory.CreateLogger<ProductService>();

// ❌ INCORRECT
var Products = await _productRepository.GetAllAsync(cancellationToken);  // Wrong case
var prods = await _productRepository.GetAllAsync(cancellationToken);  // Not descriptive
List<ProductDto> ProductDtos = products.Adapt<List<ProductDto>>();  // Wrong case
```

### Loop Variables

- **MUST** use camelCase
- **SHOULD** use singular noun for single items, plural for collections

```csharp
// ✅ CORRECT
foreach (var product in products)
{
    // Process product
}

for (int i = 0; i < products.Count; i++)
{
    var product = products[i];
    // Process product
}

// ❌ INCORRECT
foreach (var Product in products)  // Wrong case
foreach (var p in products)  // Not descriptive
foreach (var products in products)  // Confusing name
```

---

## Constants

### Constant Rules

- **MUST** use PascalCase
- **MUST** use `const` for compile-time constants
- **SHOULD** use `readonly` for runtime constants
- **SHOULD** be descriptive

```csharp
// ✅ CORRECT
private const string CorsPolicyDevelopment = "AllowAll";
private const string CorsPolicyRestricted = "Restricted";
private const string HealthCheckSelfName = "self";
private const string DefaultCspPolicy = "default-src 'self'; frame-ancestors 'none'";
private const int MaxErrorContentSize = 10 * 1024; // 10KB
public const string DefaultPolicy = "Default";

// ❌ INCORRECT
private const string corsPolicyDevelopment = "AllowAll";  // Wrong case
private const string CORS_POLICY_DEVELOPMENT = "AllowAll";  // Wrong style (should be PascalCase)
private const string Policy = "AllowAll";  // Not descriptive
```

---

## DTOs (Data Transfer Objects)

### DTO Naming Patterns

- **MUST** use PascalCase
- **MUST** use `Dto` suffix (not `DTO`, `DataTransferObject`)
- **MUST** follow these patterns:
  - `{Entity}Dto` for read/response DTOs (e.g., `ProductDto`, `CustomerDto`)
  - `Create{Entity}Dto` for create request DTOs (e.g., `CreateProductDto`, `CreateCustomerDto`)
  - `Update{Entity}Dto` for update request DTOs (e.g., `UpdateProductDto`, `UpdateCustomerDto`)

```csharp
// ✅ CORRECT
public class ProductDto { }
public class CreateProductDto { }
public class UpdateProductDto { }
public class CustomerDto { }
public class CreateCustomerDto { }
public class UpdateCustomerDto { }
public class OrderDto { }
public class OrderPositionDto { }

// ❌ INCORRECT
public class ProductDTO { }  // Should be Dto
public class ProductDataTransferObject { }  // Too verbose
public class CreateProduct { }  // Missing Dto suffix
public class ProductCreateDto { }  // Wrong order (should be CreateProductDto)
```

### Request/Response Models

- **MUST** use descriptive names
- **SHOULD** indicate purpose (e.g., `BatchUpdateRequest<T>`, `SsoAuthResponse`, `ClearCacheByKeysRequest`)

```csharp
// ✅ CORRECT
public class BatchUpdateRequest<T> { }
public class SsoAuthResponse { }
public class ClearCacheByKeysRequest { }
public class ClearCacheByTagRequest { }

// ❌ INCORRECT
public class BatchUpdate<T> { }  // Should indicate it's a request
public class SsoResponse { }  // Not descriptive enough
```

---

## Extension Method Classes

### Extension Class Names

- **MUST** use PascalCase
- **MUST** end with `Extensions` suffix
- **MUST** be static classes

```csharp
// ✅ CORRECT
public static class ServiceExtensions { }
public static class ApiVersioningExtensions { }
public static class CorsExtensions { }
public static class MiddlewareExtensions { }
public static class ControllerExtensions { }

// ❌ INCORRECT
public static class ServiceExtension { }  // Should be plural
public static class Extensions { }  // Too generic
public static class ServiceHelper { }  // Should use Extensions suffix
```

### Method Names

- **MUST** use PascalCase
- **MUST** be descriptive
- **SHOULD** start with verb (e.g., `Configure`, `Use`, `Add`)

```csharp
// ✅ CORRECT
public static void ConfigureApiServices(this IServiceCollection services, IConfiguration configuration)
public static void ConfigureCors(this IServiceCollection services, IConfiguration configuration)
public static void UseExceptionHandling(this IApplicationBuilder app, Action<ExceptionHandlingOptions> configureOptions)
public static void ValidateDatabaseConnections(this IApplicationBuilder app)

// ❌ INCORRECT
public static void configureApiServices(this IServiceCollection services)  // Wrong case
public static void ApiServices(this IServiceCollection services)  // Not a verb
```

---

## Generic Types

### Type Parameters

Following [Microsoft's type parameter naming guidelines](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names):

- **MUST** use PascalCase
- **SHOULD** use descriptive names, unless a single letter name is completely self-explanatory
- **SHOULD** use `T` as the type parameter name for types with a single type parameter
- **MUST** prefix descriptive type parameter names with "T" (e.g., `TSession`, `TKey`, `TValue`)
- **SHOULD** indicate constraints placed on a type parameter in the name (e.g., `TSession` for a parameter constrained to `ISession`)

```csharp
// ✅ CORRECT (Single letter - T is preferred)
public interface IComparer<T> { }
public delegate bool Predicate<T>(T item);
public struct Nullable<T> where T : struct { }
public interface IRepository<T> where T : BaseEntity

// ✅ CORRECT (Descriptive names with T prefix)
public interface ISessionChannel<TSession>
{
    TSession Session { get; }
}

public delegate TOutput Converter<TInput, TOutput>(TInput from);
public interface IDictionary<TKey, TValue>
public class BatchUpdateRequest<TUpdateDto>

// ❌ INCORRECT
public interface IRepository<t> where t : BaseEntity  // Wrong case
public interface IRepository<Type> where Type : BaseEntity  // Should be T for single parameter
public interface ISessionChannel<Session>  // Should be TSession (prefix with T)
public class Repository<Entity> : IRepository<Entity>  // Should be T or TEntity
```

---

## File and Folder Names

### File Names

- **MUST** match class/interface name exactly
- **MUST** use PascalCase
- **MUST** use `.cs` extension

```csharp
// ✅ CORRECT
// File: ProductDto.cs
public class ProductDto { }

// File: IProductService.cs
public interface IProductService { }

// File: ProductService.cs
public class ProductService : IProductService { }

// File: ServiceExtensions.cs
public static class ServiceExtensions { }

// ❌ INCORRECT
// File: productDto.cs  // Wrong case
// File: ProductDTO.cs  // Class name is ProductDto, not ProductDTO
// File: IProductService.cs but class name is ProductService  // Mismatch
```

### Folder Names

- **MUST** use PascalCase
- **MUST** match namespace organization
- **SHOULD** be singular for single-item folders (e.g., `Entity`, `Model`)
- **SHOULD** be plural for collections (e.g., `Entities`, `DTOs`, `Services`)

```csharp
// ✅ CORRECT
src/WebShop.Core/Entities/
src/WebShop.Core/Interfaces/Base/
src/WebShop.Core/Interfaces/Services/
src/WebShop.Business/DTOs/
src/WebShop.Business/Services/
src/WebShop.Infrastructure/Repositories/Base/
src/WebShop.Infrastructure/Services/External/
src/WebShop.Api/Extensions/Features/

// ❌ INCORRECT
src/WebShop.Core/entities/  // Wrong case
src/WebShop.Core/Entity/  // Should be plural
src/WebShop.Business/Dtos/  // Should be DTOs (industry standard)
```

---

## Special Cases

### Attributes

- **MUST** end with the word `Attribute` (e.g., `ApiControllerAttribute`, `HttpPostAttribute`)
- **MAY** omit the `Attribute` suffix when using the attribute (e.g., `[ApiController]` instead of `[ApiControllerAttribute]`)

```csharp
// ✅ CORRECT
[AttributeUsage(AttributeTargets.Class)]
public class ApiControllerAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Method)]
public class HttpPostAttribute : Attribute { }

// Usage (Attribute suffix can be omitted)
[ApiController]
[HttpPost]
public class ProductController { }

// ❌ INCORRECT
public class ApiController : Attribute { }  // Should end with Attribute
public class HttpPost : Attribute { }  // Should end with Attribute
```

### Abbreviations

- **MUST** use industry-standard abbreviations:
  - `Id` (not `ID`)
  - `Dto` (not `DTO`)
  - `Api` (not `API`)
  - `Http` (not `HTTP`)
  - `Ssl` (not `SSL`)
  - `Url` (not `URL`)
  - `Sso` (not `SSO`)
  - `Mis` (not `MIS`)
  - `Asm` (not `ASM`)

```csharp
// ✅ CORRECT
public int Id { get; set; }
public class ProductDto { }
public class BaseApiController { }
public class HttpServiceBase { }
public interface ISsoService { }

// ❌ INCORRECT
public int ID { get; set; }  // Should be Id
public class ProductDTO { }  // Should be Dto
public class BaseAPIController { }  // Should be Api
```

### Boolean Properties/Methods

- **MUST** use `Is`, `Has`, `Can` prefix
- **MUST** use positive naming (avoid `IsNot`, `HasNo`)
- **Exception**: Configuration model properties may use `Enabled` (e.g., `Enabled`, `EnabledTracing`, `EnabledMetrics`) as this follows .NET configuration conventions
- **Exception**: Domain-specific properties that match database schema may retain their existing names if changing would cause breaking changes (e.g., `CurrentlyActive` in entities/DTOs that map to database columns)

```csharp
// ✅ CORRECT
public bool IsActive { get; set; }
public bool HasPermission { get; set; }
public bool CanEdit { get; set; }
public bool IsValid { get; set; }
public bool IsSuccess { get; set; }
public async Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default)

// ✅ ACCEPTABLE (Configuration Models)
public bool Enabled { get; set; }  // Standard .NET configuration pattern
public bool EnabledTracing { get; set; }
public bool EnabledMetrics { get; set; }

// ⚠️ ACCEPTABLE (Domain Properties Matching Database Schema)
public bool? CurrentlyActive { get; set; }  // Matches database column name

// ❌ INCORRECT
public bool Active { get; set; }  // Should be IsActive
public bool isActive { get; set; }  // Wrong case
public bool IsNotActive { get; set; }  // Should be !IsActive
public bool HasNoPermission { get; set; }  // Should be !HasPermission
public bool Success { get; set; }  // Should be IsSuccess (unless in result DTO where Success is standard)
```

### Collections

- **MUST** use descriptive plural names
- **SHOULD** indicate collection type when needed (e.g., `productList`, `productArray`)

```csharp
// ✅ CORRECT
IReadOnlyList<ProductDto> products = await _service.GetAllAsync(cancellationToken);
List<Product> productList = new();
Dictionary<int, Product> productLookup = products.ToDictionary(p => p.Id);
IEnumerable<Customer> customers = await _repository.FindAsync(c => c.IsActive, cancellationToken);

// ❌ INCORRECT
IReadOnlyList<ProductDto> product = await _service.GetAllAsync(cancellationToken);  // Should be plural
List<Product> Products = new();  // Wrong case
Dictionary<int, Product> dict = products.ToDictionary(p => p.Id);  // Not descriptive
```

### Async/Await Patterns

- **MUST** use `Async` suffix for async methods
- **MUST** use `await` for async calls
- **MUST** use `ConfigureAwait(false)` in library code

```csharp
// ✅ CORRECT
public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
{
    Product? product = await _repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
    return product?.Adapt<ProductDto>();
}

// ❌ INCORRECT
public async Task<ProductDto?> GetById(int id)  // Missing Async suffix
{
    Product? product = _repository.GetByIdAsync(id).Result;  // Should use await
    return product?.Adapt<ProductDto>();
}
```

---

## Examples

### Complete Example: Service Class

```csharp
namespace WebShop.Business.Services;

/// <summary>
/// Service for managing products.
/// </summary>
public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    public ProductService(
        IProductRepository productRepository,
        ILogger<ProductService> logger)
    {
        _productRepository = productRepository ?? throw new ArgumentNullException(nameof(productRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Product? product = await _productRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        
        if (product == null)
        {
            _logger.LogWarning("Product with ID {ProductId} not found", id);
            return null;
        }

        return product.Adapt<ProductDto>();
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Product> products = await _productRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return products.Adapt<IReadOnlyList<ProductDto>>();
    }
}
```

### Complete Example: Extension Method

```csharp
namespace WebShop.Api.Extensions.Features;

/// <summary>
/// Extension methods for configuring API versioning.
/// </summary>
public static class ApiVersioningExtensions
{
    private const string DefaultApiVersion = "1";

    /// <summary>
    /// Configures API versioning for the application.
    /// </summary>
    public static void ConfigureApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1);
            options.AssumeDefaultVersionWhenUnspecified = true;
        });
    }
}
```

### Complete Example: DTO

```csharp
namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for product information.
/// </summary>
public class ProductDto
{
    /// <summary>
    /// Unique identifier for the product.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the product.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Indicates if the product record is active.
    /// </summary>
    public bool IsActive { get; set; }
}
```

---

## Common Mistakes to Avoid

### ❌ Wrong Case

```csharp
// ❌ INCORRECT
public class productService { }
public int customerId { get; set; }
public void getById(int id) { }

// ✅ CORRECT
public class ProductService { }
public int CustomerId { get; set; }
public void GetById(int id) { }
```

### ❌ Missing Async Suffix

```csharp
// ❌ INCORRECT
public async Task<ProductDto> GetById(int id) { }

// ✅ CORRECT
public async Task<ProductDto> GetByIdAsync(int id, CancellationToken cancellationToken = default) { }
```

### ❌ Wrong Interface Naming

```csharp
// ❌ INCORRECT
public interface ProductService { }
public interface IProductServices { }

// ✅ CORRECT
public interface IProductService { }
```

### ❌ Wrong Field Naming

```csharp
// ❌ INCORRECT
private readonly IProductRepository productRepository;
private readonly ILogger<ProductService> Logger;

// ✅ CORRECT
private readonly IProductRepository _productRepository;
private readonly ILogger<ProductService> _logger;
```

### ❌ Wrong Parameter Naming

```csharp
// ❌ INCORRECT
public async Task<ProductDto> CreateAsync(CreateProductDto Dto, CancellationToken cancellationToken) { }

// ✅ CORRECT
public async Task<ProductDto> CreateAsync(CreateProductDto createDto, CancellationToken cancellationToken = default) { }
```

### ❌ Wrong DTO Naming

```csharp
// ❌ INCORRECT
public class ProductDTO { }
public class CreateProduct { }
public class ProductUpdateDto { }

// ✅ CORRECT
public class ProductDto { }
public class CreateProductDto { }
public class UpdateProductDto { }
```

---

## Related Documentation

- [Microsoft C# Identifier Naming Rules and Conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names) - Official Microsoft guidelines (takes precedence)
- [.NET Runtime Team Coding Style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md) - Additional .NET conventions
- [Project Structure Guide](../architecture/project-structure.md) - Folder and namespace organization
- [XML Comments Guidelines](xml-comments-guidelines.md) - Documentation conventions

---

## Summary

This codebase follows [Microsoft's official C# naming conventions](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names) with the following key principles:

1. **PascalCase** for all public members (classes, interfaces, methods, properties, namespaces)
2. **camelCase** for private instance fields (with `_` prefix), static mutable fields (with `_` prefix)
3. **PascalCase** for constants and static readonly immutable fields (collections, dictionaries, arrays, Regex patterns)
4. **Async suffix** for all async methods (except controller actions - optional)
5. **I prefix** for all interfaces
6. **Attribute suffix** for all attribute classes
7. **Dto suffix** for all data transfer objects
8. **Extensions suffix** for extension method classes
9. **Singular nouns** for enum types (non-flags), **plural nouns** for flags enums
10. **Type parameters**: `T` for single parameter, `TName` for descriptive names
11. **Primary constructors**: camelCase for classes/structs, PascalCase for records
12. **Descriptive names** that clearly indicate purpose (prefer clarity over brevity)
13. **Consistent abbreviations** (Id, Dto, Api, Http, Ssl, Url, Sso, Mis, Asm)
14. **No consecutive underscores** (`__`) - reserved for compiler-generated identifiers

**Reference:** These guidelines are based on and aligned with [Microsoft's official documentation](https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/identifier-names), with Microsoft's guidelines taking precedence when there are conflicts.

Following these guidelines ensures code consistency, improves readability, and makes the codebase easier to maintain and understand for all developers.
