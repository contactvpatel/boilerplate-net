# Project Structure Guide

This document explains the organization and structure of the WebShop .NET API project, including the separation of base and resource-specific components.

## Overview

The project follows **Clean Architecture** principles with clear separation of concerns across multiple layers. Recent improvements have reorganized the codebase to better separate base components from resource-specific implementations.

### One Type Per File Principle

The codebase strictly adheres to the **"One Type Per File"** principle, a C# best practice where each file contains a single public type (class, interface, enum, or struct). This approach provides:

- **Better Discoverability**: Easy to locate types by file name
- **Improved Version Control**: Smaller, focused file changes
- **Reduced Merge Conflicts**: Multiple developers can work on different types simultaneously
- **Enhanced Code Navigation**: IDE tools work more efficiently with single-type files
- **Clear Organization**: File name matches type name exactly

**Examples**:

- `IDapperConnectionFactory.cs` contains only the `IDapperConnectionFactory` interface
- `RateLimitPolicy.cs` contains only the `RateLimitPolicy` class (split from `RateLimitingOptions.cs`)

## Project Organization

```
boilerplate-net/
├── src/
│   ├── WebShop.Api/              # Presentation Layer
│   │   ├── Controllers/          # API Controllers
│   │   ├── Filters/              # Action Filters (Validation, JWT Auth)
│   │   ├── Middleware/           # Custom Middleware
│   │   ├── Extensions/           # Service Configuration Extensions
│   │   │   ├── Core/            # Core Extensions
│   │   │   ├── Features/        # Feature Extensions
│   │   │   ├── Middleware/      # Middleware Extensions
│   │   │   └── Utilities/       # Utility Extensions
│   │   └── Models/               # API Request/Response Models
│   │
│   ├── WebShop.Business/         # Application Layer
│   │   ├── Services/            # Business Services
│   │   │   ├── Interfaces/      # Business Layer Service Interfaces
│   │   │   └── *.cs             # Service Implementations
│   │   ├── DTOs/                # Data Transfer Objects
│   │   ├── Mappings/            # Mapster Mapping Configuration
│   │   └── Validators/           # FluentValidation Validators
│   │
│   ├── WebShop.Core/             # Domain Layer
│   │   ├── Entities/            # Domain Entities
│   │   ├── Interfaces/           # Domain Interfaces
│   │   │   ├── Base/            # Base Interfaces
│   │   │   │   ├── IRepository.cs
│   │   │   │   ├── IUserContext.cs
│   │   │   │   └── ICacheService.cs
│   │   │   ├── Services/        # Service Interfaces
│   │   │   │   ├── ISsoService.cs
│   │   │   │   ├── IMisService.cs
│   │   │   │   └── IAsmService.cs
│   │   │   └── *.cs             # Resource Repository Interfaces
│   │   │       ├── IProductRepository.cs
│   │   │       ├── ICustomerRepository.cs
│   │   │       └── ...
│   │   └── Models/              # Domain Models
│   │
│   ├── WebShop.Infrastructure/   # Infrastructure Layer
│   │   ├── Data/                # Transaction Management
│   │   ├── Repositories/        # Repository Implementations
│   │   │   ├── Base/            # Base Repository Classes
│   │   │   │   └── DapperRepositoryBase.cs
│   │   │   └── *.cs            # Entity-Specific Repository Implementations (Dapper)
│   │   │       ├── ProductRepository.cs
│   │   │       ├── CustomerRepository.cs
│   │   │       └── ...
│   │   ├── Services/            # Infrastructure Services
│   │   │   ├── External/        # External HTTP Services
│   │   │   │   ├── AsmService.cs
│   │   │   │   ├── MisService.cs
│   │   │   │   └── SsoService.cs
│   │   │   └── Internal/        # Internal Infrastructure Services
│   │   │       ├── CacheService.cs
│   │   │       └── UserContext.cs
│   │   ├── Interfaces/          # Infrastructure Layer Interfaces
│   │   │   ├── IDapperConnectionFactory.cs
│   │   │   └── IDapperTransactionManager.cs
│   │   └── Helpers/             # Infrastructure Helpers (Implementations)
│   │       ├── DapperConnectionFactory.cs
│   │       ├── DapperTransactionManager.cs
│   │       ├── DapperQueryBuilder.cs
│   │       ├── DbConnectionStringCache.cs
│   │       ├── HttpServiceBase.cs
│   │       ├── HttpErrorHandler.cs
│   │       ├── HttpClientExtensions.cs
│   │       ├── JsonContext.cs
│   │       ├── SensitiveDataSanitizer.cs
│   │       └── UrlValidator.cs
│   │
│   └── WebShop.Util/            # Utilities Layer
│       ├── Models/              # Utility Models (Configuration Options)
│       │   ├── AsmServiceOptions.cs
│       │   ├── AsmServiceEndpoints.cs
│       │   ├── MisServiceOptions.cs
│       │   ├── MisServiceHeaders.cs
│       │   ├── MisServiceEndpoints.cs
│       │   ├── SsoServiceOptions.cs
│       │   ├── SsoServiceEndpoints.cs
│       │   ├── DbConnectionModel.cs
│       │   ├── ConnectionModel.cs
│       │   ├── RateLimitingOptions.cs
│       │   ├── RateLimitPolicy.cs
│       │   └── ... (each type in its own file)
│       └── Security/            # Security Utilities
│
├── docs/                        # Documentation
├── scripts/                      # Build and Setup Scripts
└── WebShop.slnx                 # Solution File
```

## Interface Organization

### Base Interfaces (`WebShop.Core.Interfaces.Base`)

Base interfaces provide fundamental abstractions used throughout the application:

- **`IRepository<T>`**: Generic repository interface for entities inheriting from `BaseEntity`
- **`IUserContext`**: Interface for accessing current authenticated user information
- **`ICacheService`**: Cache service interface for caching operations

**Location**: `src/WebShop.Core/Interfaces/Base/`

**Usage Example**:

```csharp
using WebShop.Core.Interfaces.Base;

public class MyService(ICacheService cacheService, IUserContext userContext)
{
    // Use base interfaces
}
```

### Service Interfaces (`WebShop.Core.Interfaces.Services`)

Service interfaces define contracts for external service integrations:

- **`ISsoService`**: Single Sign-On service interface
- **`IMisService`**: Management Information System service interface
- **`IAsmService`**: Application Security Management service interface

**Location**: `src/WebShop.Core/Interfaces/Services/`

**Usage Example**:

```csharp
using WebShop.Core.Interfaces.Services;

public class MyService(ISsoService ssoService)
{
    // Use service interfaces
}
```

### Resource Repository Interfaces (`WebShop.Core.Interfaces`)

Resource-specific repository interfaces extend the base `IRepository<T>` interface:

- **`IProductRepository`**: Product-specific repository operations
- **`ICustomerRepository`**: Customer-specific repository operations
- **`IAddressRepository`**: Address-specific repository operations
- And other resource-specific interfaces...

**Location**: `src/WebShop.Core/Interfaces/`

**Usage Example**:

```csharp
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;

public interface IProductRepository : IRepository<Product>
{
    Task<List<Product>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default);
}
```

## Repository Organization

### Base Repositories (`WebShop.Infrastructure.Repositories.Base`)

Base repository classes provide foundation for Dapper operations:

- **`DapperRepositoryBase<T>`**: ✅ **Hybrid approach** - Direct Dapper for reads, shared helpers for writes (3-5x faster than reflection-based approaches)

**Location**: `src/WebShop.Infrastructure/Repositories/Base/`

**DapperRepositoryBase<T> Features** (Recommended):

- ✅ **Zero reflection overhead** - Direct Dapper mapping for peak performance
- ✅ **3-5x faster reads** compared to generic repository
- ✅ `IDapperConnectionFactory` for read/write connection separation
- ✅ `IDapperTransactionManager` for transaction support
- ✅ Shared write operations (`AddAsync`, `UpdateAsync`, `DeleteAsync`)
- ✅ Consistent audit field management
- ✅ Explicit SQL queries with parameterization
- ✅ Soft delete support (sets `IsActive = false`)
- ✅ Connection lifecycle management
- ✅ Each repository implements reads with direct Dapper for optimal performance

**Migration Status**:

- ✅ **All repositories migrated** to hybrid approach (Dapper direct SQL for reads)
  - `CustomerRepository` (reference implementation)
  - `ProductRepository`
  - `OrderRepository`
  - `ArticleRepository`
  - `AddressRepository`
  - `OrderPositionRepository`
  - `LabelRepository`
  - `ColorRepository`
  - `SizeRepository`
  - `StockRepository`

### Entity-Specific Repositories (`WebShop.Infrastructure.Repositories`)

Entity-specific repository implementations extend `DapperRepositoryBase<T>` (hybrid approach):

- **`CustomerRepository`**: Customer-specific queries and operations
- **`ProductRepository`**: Product-specific queries and operations
- **`OrderRepository`**: Order-specific queries and operations
- **`ArticleRepository`**: Article-specific queries and operations
- **`AddressRepository`**: Address-specific queries and operations
- **`OrderPositionRepository`**: Order position-specific queries and operations
- **`LabelRepository`**: Label-specific queries and operations
- **`ColorRepository`**: Color-specific queries and operations
- **`SizeRepository`**: Size-specific queries and operations
- **`StockRepository`**: Stock-specific queries and operations

**Location**: `src/WebShop.Infrastructure/Repositories/`

**Example (Hybrid Approach - Recommended)**:

```csharp
using WebShop.Infrastructure.Interfaces;
using WebShop.Infrastructure.Repositories.Base;

namespace WebShop.Infrastructure.Repositories;

// ✅ Hybrid approach: Direct Dapper for reads, base class for writes
public class CustomerRepository : DapperRepositoryBase<Customer>, ICustomerRepository
{
    protected override string TableName => "customer";

    public CustomerRepository(
        IDapperConnectionFactory connectionFactory,
        IDapperTransactionManager? transactionManager = null,
        ILoggerFactory? loggerFactory = null)
        : base(connectionFactory, transactionManager, loggerFactory)
    {
    }

    // ✅ READ: Direct Dapper mapping (3-5x faster, zero reflection)
    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""firstname"" AS FirstName,
                ""lastname"" AS LastName,
                ""email"" AS Email,
                ""created"" AS CreatedAt,
                ""isactive"" AS IsActive
            FROM ""webshop"".""customer""
            WHERE ""id"" = @Id AND ""isactive"" = true";

        using var connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Customer>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken));
    }

    // ✅ READ: Custom query with direct Dapper
    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT 
                ""id"" AS Id,
                ""firstname"" AS FirstName,
                ""email"" AS Email
            FROM ""webshop"".""customer""
            WHERE ""email"" = @Email AND ""isactive"" = true";

        using var connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Customer>(
            new CommandDefinition(sql, new { Email = email }, cancellationToken: cancellationToken));
    }

    // ✅ WRITE: Inherited from DapperRepositoryBase<T>
    // AddAsync(), UpdateAsync(), DeleteAsync() provided by base class

    protected override string BuildInsertSql()
    {
        return @"
            INSERT INTO ""webshop"".""customer"" 
            (""firstname"", ""lastname"", ""email"", ""created"", ""isactive"")
            VALUES (@FirstName, @LastName, @Email, @CreatedAt, @IsActive)
            RETURNING ""id""";
    }

    protected override string BuildUpdateSql()
    {
        return @"
            UPDATE ""webshop"".""customer""
            SET ""firstname"" = @FirstName, ""lastname"" = @LastName, 
                ""email"" = @Email, ""updated"" = @UpdatedAt
            WHERE ""id"" = @Id AND ""isactive"" = true";
    }
}
```

**See also:** [Dapper Hybrid Approach Guide](dapper-hybrid-approach.md) for complete implementation patterns

### Infrastructure Interfaces (`WebShop.Infrastructure.Interfaces`)

Infrastructure-specific interfaces that define contracts for infrastructure components:

- **`IDapperConnectionFactory`**: Interface for creating database connections with read/write separation
- **`IDapperTransactionManager`**: Interface for managing database transactions

**Location**: `src/WebShop.Infrastructure/Interfaces/`

**Design Decision**: These interfaces are placed in the Infrastructure layer (not Core) because:

- They are infrastructure concerns specific to Dapper implementation
- They use Dapper types (`IDbConnection`, `IDbTransaction`)
- Repositories in Infrastructure depend on these for data access
- They don't represent domain abstractions

**Usage Example**:

```csharp
using WebShop.Infrastructure.Interfaces;

public class MyRepository
{
    private readonly IDapperConnectionFactory _connectionFactory;
    private readonly IDapperTransactionManager _transactionManager;

    public MyRepository(
        IDapperConnectionFactory connectionFactory,
        IDapperTransactionManager transactionManager)
    {
        _connectionFactory = connectionFactory;
        _transactionManager = transactionManager;
    }
}
```

### Dapper Helpers (`WebShop.Infrastructure.Helpers`)

Helper classes provide reusable functionality for Dapper operations:

**Query Building & Execution**:

- **`DapperQueryBuilder`**: Builds secure, parameterized SQL queries (SELECT, INSERT, UPDATE, soft delete)

**Connection & Transaction Management** (Implementations):

- **`DapperConnectionFactory`**: Implements `IDapperConnectionFactory` for creating database connections with read/write separation
- **`DapperTransactionManager`**: Implements `IDapperTransactionManager` for managing database transactions with automatic commit/rollback

**Location**: `src/WebShop.Infrastructure/Helpers/`

**Key Features**:

- **SQL Injection Protection**: All queries use parameterized statements
- **Explicit Column Lists**: No `SELECT *` queries
- **PostgreSQL Identifier Quoting**: Prevents identifier-based injection
- **Transaction Support**: Full transaction management via `IDapperTransactionManager`
- **Connection Pooling**: Efficient connection reuse via Npgsql
- **One Type Per File**: Each interface and implementation in separate files

**Example Usage**:

```csharp
// Build a parameterized SELECT query
string sql = DapperQueryBuilder.BuildSelectQuery(
    tableName: "webshop.products",
    columns: "id, name, category, price",
    whereClause: "category = @Category AND isactive = true");

// Use connection factory
IDbConnection connection = _connectionFactory.CreateReadConnection();
IEnumerable<Product> products = await connection.QueryAsync<Product>(sql, new { Category = "Electronics" });
```

## Infrastructure Services (`WebShop.Infrastructure.Services`)

Infrastructure services are organized into two categories:

### External Services (`WebShop.Infrastructure.Services.External`)

External HTTP services that communicate with external APIs:

- **`SsoService`**: Single Sign-On service for token validation and authentication
- **`MisService`**: Management Information System service for organizational data
- **`AsmService`**: Application Security Management service for authorization

**Location**: `src/WebShop.Infrastructure/Services/External/`

**Features**:

- All services extend `HttpServiceBase` for common HTTP functionality
- Use named HttpClient instances with resilience policies
- Implement retry, circuit breaker, and timeout strategies
- SSRF protection via `UrlValidator`

### Internal Services (`WebShop.Infrastructure.Services.Internal`)

Internal infrastructure services used within the application:

- **`CacheService`**: HybridCache wrapper with stampede protection and tag-based invalidation
- **`UserContext`**: Provides access to current authenticated user information

**Location**: `src/WebShop.Infrastructure/Services/Internal/`

## Infrastructure Interfaces (`WebShop.Infrastructure.Interfaces`)

Infrastructure-specific interfaces for data access components:

- **`IDapperConnectionFactory`**: Interface for creating database connections with read/write separation
- **`IDapperTransactionManager`**: Interface for managing database transactions

**Location**: `src/WebShop.Infrastructure/Interfaces/`

**Why Infrastructure Layer?**

These interfaces belong in the Infrastructure layer (not Core) because:

- They represent infrastructure concerns specific to Dapper implementation
- They use Dapper-specific types (`IDbConnection`, `IDbTransaction`)
- They are implementation details, not domain abstractions
- Repositories in Infrastructure depend on these for data access

## Infrastructure Helpers (`WebShop.Infrastructure.Helpers`)

Helper classes provide reusable functionality for infrastructure operations:

**Dapper Helpers** (Implementations):

- **`DapperConnectionFactory`**: Implements `IDapperConnectionFactory` for creating database connections with read/write separation
- **`DapperTransactionManager`**: Implements `IDapperTransactionManager` for managing database transactions with automatic commit/rollback
- **`DapperQueryBuilder`**: Builds secure, parameterized SQL queries

**Database Helpers**:

- **`DbConnectionStringCache`**: Caches database connection strings to avoid recreation

**HTTP Service Helpers**:

- **`HttpServiceBase`**: Base class for HTTP service implementations with common functionality
- **`HttpErrorHandler`**: Handles HTTP errors and exceptions for external service calls
- **`HttpClientExtensions`**: Extension methods for HttpClient configuration and resilience

**Security & Utility Helpers**:

- **`JsonContext`**: JSON source generator context for optimized serialization/deserialization
- **`SensitiveDataSanitizer`**: Sanitizes sensitive data (passwords, tokens, credit cards) in logs
- **`UrlValidator`**: Validates external service URLs to prevent SSRF attacks

**Location**: `src/WebShop.Infrastructure/Helpers/`

**Key Features**:

- **One Type Per File**: Each interface and implementation in separate files
- **SQL Injection Protection**: Parameterized queries via `DapperQueryBuilder`
- **SSRF Protection**: `UrlValidator` prevents localhost/internal network access
- **Performance**: `JsonContext` uses source generation for faster JSON operations
- **Security**: `SensitiveDataSanitizer` masks sensitive data in logs
- **Resilience**: `HttpServiceBase` provides common resilience patterns
- **Transaction Management**: Full transaction support via `IDapperTransactionManager`

## Utilities Layer (`WebShop.Util`)

The Utilities layer contains shared configuration models and security utilities used across the application.

### Utility Models (`WebShop.Util.Models`)

Configuration option models organized following the **One Type Per File** principle:

**External Service Options**:

- **`AsmServiceOptions`**: ASM service configuration
- **`AsmServiceEndpoints`**: ASM service endpoint paths
- **`MisServiceOptions`**: MIS service configuration
- **`MisServiceHeaders`**: MIS service authentication headers
- **`MisServiceEndpoints`**: MIS service endpoint paths
- **`SsoServiceOptions`**: SSO service configuration
- **`SsoServiceEndpoints`**: SSO service endpoint paths

**Database Configuration**:

- **`DbConnectionModel`**: Database connection settings container
- **`ConnectionModel`**: Individual database connection settings

**Rate Limiting**:

- **`RateLimitingOptions`**: Rate limiting configuration container
- **`RateLimitPolicy`**: Individual rate limit policy settings

**API Configuration**:

- **`ApiVersionDeprecationOptions`**: API version deprecation configuration
- **`DeprecatedVersion`**: Individual deprecated version settings

**Location**: `src/WebShop.Util/Models/`

**Organization Pattern**:

When a configuration class contains nested types, they are split into separate files:

```csharp
// Before (One file with nested types)
public class ServiceOptions
{
    public EndpointsConfig Endpoints { get; set; }
    
    public class EndpointsConfig  // Nested type
    {
        public string BaseUrl { get; set; }
    }
}

// After (Separate files following One Type Per File)
// File: ServiceOptions.cs
public class ServiceOptions
{
    public ServiceEndpoints Endpoints { get; set; }
}

// File: ServiceEndpoints.cs
public class ServiceEndpoints
{
    public string BaseUrl { get; set; }
}
```

**Benefits**:

- Each type is easily discoverable by file name
- Better version control with smaller, focused files
- Reduced merge conflicts when multiple developers work on configuration
- Improved IDE navigation and code search

## Benefits of This Structure

### 1. Clear Separation of Concerns

- **Base components** are isolated from resource-specific implementations
- **Service interfaces** are separated from repository interfaces
- **Helpers** are organized separately from implementations
- **Interfaces and implementations** are in separate files (One Type Per File)

### 2. Improved Maintainability

- Easy to locate base interfaces and repositories
- Resource-specific code is clearly separated
- Helper classes are easy to find for reference
- Each type has its own file for better organization

### 3. Better Scalability

- Adding new resources doesn't clutter base components
- Base components remain stable and focused
- New developers can quickly understand the structure
- One Type Per File reduces merge conflicts

### 4. Consistent Patterns

- All resource repositories follow the same pattern
- Base interfaces provide consistent contracts
- Service interfaces follow consistent naming and organization
- Every file contains exactly one public type

### 5. Enhanced Developer Experience

- **File Discovery**: Type name matches file name exactly
- **IDE Navigation**: Go-to-definition works seamlessly
- **Code Search**: Find types by file name instantly
- **Version Control**: Smaller, focused diffs and better merge tracking

## Namespace Guidelines

### Core Layer Interfaces

- **Base Interfaces**: `WebShop.Core.Interfaces.Base`
- **Service Interfaces**: `WebShop.Core.Interfaces.Services`
- **Resource Repository Interfaces**: `WebShop.Core.Interfaces`

### Infrastructure Layer

- **Infrastructure Interfaces**: `WebShop.Infrastructure.Interfaces` (Data access interfaces)
- **Base Repository**: `WebShop.Infrastructure.Repositories.Base` (DapperRepositoryBase<T>)
- **Resource Repositories**: `WebShop.Infrastructure.Repositories` (all extend DapperRepositoryBase<T>)
- **Dapper Helpers**: `WebShop.Infrastructure.Helpers` (Query builders, connection factory implementations, transaction manager)
- **External Services**: `WebShop.Infrastructure.Services.External` (HTTP services for external APIs)
- **Internal Services**: `WebShop.Infrastructure.Services.Internal` (Cache, UserContext)

### Utilities Layer

- **Utility Models**: `WebShop.Util.Models` (Configuration options, following One Type Per File)
- **Security Utilities**: `WebShop.Util.Security` (Security helpers)

## Adding New Components

### Adding a New Resource Repository

1. **Create Interface** in `src/WebShop.Core/Interfaces/`:

   ```csharp
   using WebShop.Core.Entities;
   using WebShop.Core.Interfaces.Base;

   namespace WebShop.Core.Interfaces;

   public interface IMyResourceRepository : IRepository<MyResource>
   {
       Task<List<MyResource>> GetCustomQueryAsync(/* parameters */);
   }
   ```

2. **Create Implementation** in `src/WebShop.Infrastructure/Repositories/`:

   ```csharp
   using WebShop.Infrastructure.Interfaces;
   using WebShop.Infrastructure.Repositories.Base;

   namespace WebShop.Infrastructure.Repositories;

   public class MyResourceRepository : DapperRepositoryBase<MyResource>, IMyResourceRepository
   {
       protected override string TableName => "myresources";

       public MyResourceRepository(
           IDapperConnectionFactory connectionFactory,
           IDapperTransactionManager? transactionManager = null,
           ILoggerFactory? loggerFactory = null)
           : base(connectionFactory, transactionManager, loggerFactory)
       {
       }

       // Implement read methods with direct Dapper (see CustomerRepository for reference)
       public async Task<MyResource?> GetByIdAsync(int id, CancellationToken ct = default)
       {
           const string sql = @"SELECT ""id"" AS Id, ""name"" AS Name FROM ""webshop"".""myresources"" WHERE ""id"" = @Id AND ""isactive"" = true";
           using var connection = GetReadConnection();
           return await connection.QueryFirstOrDefaultAsync<MyResource>(new CommandDefinition(sql, new { Id = id }, cancellationToken: ct));
       }

       // Write methods (AddAsync, UpdateAsync, DeleteAsync) inherited from base
       protected override string BuildInsertSql() => @"INSERT INTO ""webshop"".""myresources"" (""name"", ""isactive"", ""created"", ""createdby"") VALUES (@Name, @IsActive, @CreatedAt, @CreatedBy) RETURNING ""id""";
       protected override string BuildUpdateSql() => @"UPDATE ""webshop"".""myresources"" SET ""name"" = @Name, ""updated"" = @UpdatedAt, ""updatedby"" = @UpdatedBy WHERE ""id"" = @Id AND ""isactive"" = true";
   }
   ```

3. **Register in DependencyInjection**:

   ```csharp
   services.AddScoped<IMyResourceRepository, MyResourceRepository>();
   ```

### Adding a New Service Interface

1. **Create Interface** in `src/WebShop.Core/Interfaces/Services/`:

   ```csharp
   namespace WebShop.Core.Interfaces.Services;

   public interface IMyService
   {
       Task<MyModel?> GetDataAsync(string id, CancellationToken cancellationToken = default);
   }
   ```

2. **Create Implementation** in `src/WebShop.Infrastructure/Services/External/` or `Internal/`:

   ```csharp
   using WebShop.Core.Interfaces.Services;

   namespace WebShop.Infrastructure.Services.External; // or Internal

   public class MyService : IMyService
   {
       // Implementation
   }
   ```

3. **Register in DependencyInjection**:

   ```csharp
   services.AddScoped<WebShop.Core.Interfaces.Services.IMyService, MyService>();
   ```

## API Layer (`WebShop.Api`)

The `WebShop.Api` project is the presentation layer that handles HTTP requests and responses. It depends on `WebShop.Business` and `WebShop.Infrastructure`.

### Extensions Folder (`WebShop.Api/Extensions`)

The `Extensions` folder is organized into subfolders to categorize different types of extension methods:

```
src/WebShop.Api/Extensions/
├── Core/                                    # Core configuration extensions
│   ├── ServiceExtensions.cs                # Main service orchestrator
│   └── ConfigurationExtensions.cs        # Application settings configuration
├── Features/                                # Feature-specific extensions
│   ├── ApiVersioningExtensions.cs         # API versioning configuration
│   ├── CorsExtensions.cs                   # CORS policy configuration
│   ├── HealthCheckExtensions.cs           # Health check configuration
│   ├── OpenApiExtensions.cs               # OpenAPI/Scalar UI configuration
│   ├── RateLimitingExtensions.cs          # Rate limiting configuration
│   ├── ResponseCompressionExtensions.cs   # Response compression configuration
│   └── SecurityHeadersExtensions.cs       # Security headers configuration
├── Middleware/                             # Middleware pipeline extensions
│   ├── MiddlewareExtensions.cs           # Middleware pipeline configuration
│   └── ExceptionHandlingExtensions.cs     # Exception handling middleware
└── Utilities/                              # Utility extensions
    ├── ControllerExtensions.cs           # Controller configuration
    ├── DatabaseConnectionValidationExtensions.cs  # Database validation
    └── DbUpLoggerExtension.cs            # DbUp logger integration
```

#### Namespace Guidelines

- **Core Extensions**: `WebShop.Api.Extensions.Core`
- **Feature Extensions**: `WebShop.Api.Extensions.Features`
- **Middleware Extensions**: `WebShop.Api.Extensions.Middleware`
- **Utility Extensions**: `WebShop.Api.Extensions.Utilities`

#### Example: Adding a New Feature Extension

```csharp
// src/WebShop.Api/Extensions/Features/NewFeatureExtensions.cs
namespace WebShop.Api.Extensions.Features;

public static class NewFeatureExtensions
{
    public static void ConfigureNewFeature(this IServiceCollection services, IConfiguration configuration)
    {
        // Configuration logic
    }
}
```

#### Example: Adding a New Utility Extension

```csharp
// src/WebShop.Api/Extensions/Utilities/NewUtilityExtensions.cs
namespace WebShop.Api.Extensions.Utilities;

public static class NewUtilityExtensions
{
    public static void UseNewUtility(this IApplicationBuilder app)
    {
        // Middleware or utility logic
    }
}
```

## Coding Standards and Best Practices

### One Type Per File Principle

The codebase strictly enforces the **One Type Per File** principle:

**Rules**:

1. Each file contains exactly **one public type** (class, interface, enum, or struct)
2. File name **must match** the type name exactly
3. Nested types are **extracted** into separate files
4. Private types used only within a single file are **exceptions** (but rare)

**Examples of Compliance**:

✅ **Correct**:

```
IDapperConnectionFactory.cs     → IDapperConnectionFactory interface
DapperConnectionFactory.cs      → DapperConnectionFactory class
RateLimitPolicy.cs              → RateLimitPolicy class
AsmServiceEndpoints.cs          → AsmServiceEndpoints class
```

❌ **Incorrect**:

```
DapperConnectionFactory.cs      → IDapperConnectionFactory + DapperConnectionFactory
RateLimitingOptions.cs          → RateLimitingOptions + RateLimitPolicy
```

**How to Split Files**:

When you encounter a file with multiple types:

1. **Identify all public types** in the file
2. **Create new files** for each type (except the primary type)
3. **Move each type** to its own file with matching file name
4. **Update namespaces** if necessary (e.g., moving interfaces to `Interfaces/` folder)
5. **Add using directives** in dependent files
6. **Verify build** to ensure no errors

**Example - Splitting Configuration Options**:

```csharp
// Before: MisServiceOptions.cs (3 types in one file)
public class MisServiceOptions
{
    public MisServiceHeaders Headers { get; set; }
    public MisServiceEndpoints Endpoints { get; set; }
}
public class MisServiceHeaders { ... }
public class MisServiceEndpoints { ... }

// After: Three separate files
// File: MisServiceOptions.cs
public class MisServiceOptions
{
    public MisServiceHeaders Headers { get; set; }
    public MisServiceEndpoints Endpoints { get; set; }
}

// File: MisServiceHeaders.cs
public class MisServiceHeaders { ... }

// File: MisServiceEndpoints.cs
public class MisServiceEndpoints { ... }
```

### Interface and Implementation Separation

Interfaces and their implementations are placed in separate files:

**Pattern**:

- **Interface**: `I{TypeName}.cs` in `Interfaces/` folder
- **Implementation**: `{TypeName}.cs` in appropriate folder (`Helpers/`, `Services/`, `Repositories/`)

**Examples**:

```
src/WebShop.Infrastructure/
├── Interfaces/
│   ├── IDapperConnectionFactory.cs
│   └── IDapperTransactionManager.cs
└── Helpers/
    ├── DapperConnectionFactory.cs
    └── DapperTransactionManager.cs
```

**Benefits**:

- Clear contract definition (interface) separate from implementation
- Easy to locate interfaces for dependency injection
- Supports multiple implementations of the same interface
- Better testability with mocking frameworks

## Related Documentation

- [Dapper Hybrid Approach](dapper-hybrid-approach.md) - High-performance data access with direct Dapper mapping
- [Dapper Testing Guide](dapper-testing-guide.md) - Testing with mocked connections
- [HttpClient Factory Guide](httpclient-factory.md) - Creating HTTP services
- [Performance Optimization Guide](performance-optimization-guide.md) - Performance best practices
- [Architecture & Patterns](../README.md#architecture--patterns) - Clean Architecture overview
