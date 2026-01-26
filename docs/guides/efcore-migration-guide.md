# EF Core Migration Guide: From Dapper to Entity Framework Core

This comprehensive guide covers migrating from the current Dapper-based architecture to Entity Framework Core while maintaining high performance and security. It addresses common EF Core pitfalls including ORM overhead, N+1 queries, Cartesian explosion, and security vulnerabilities.

[← Back to README](../../README.md)

## Table of Contents

- [Overview](#overview)
- [Migration Prerequisites](#migration-prerequisites)
- [EF Core Setup and Configuration](#ef-core-setup-and-configuration)
- [Performance Optimization Strategies](#performance-optimization-strategies)
- [Security Best Practices](#security-best-practices)
- [Bulk Operations in EF Core](#bulk-operations-in-ef-core)
- [Code Migration Process](#code-migration-process)
- [Common EF Core Issues and Solutions](#common-ef-core-issues-and-solutions)
- [Testing and Validation](#testing-and-validation)
- [Performance Comparison](#performance-comparison)

---

## Overview

### Current Architecture (Dapper-based)

**Strengths:**

- ✅ **High Performance**: Direct SQL execution with minimal overhead
- ✅ **Explicit Control**: Full control over SQL queries and execution
- ✅ **Predictable Behavior**: No unexpected queries or lazy loading
- ✅ **Resource Efficient**: Minimal memory footprint

**Current Implementation:**

- `DapperRepositoryBase<T>`: Shared write operations
- Direct Dapper queries for reads: `GetByIdAsync`, `GetAllAsync`, `GetPagedAsync`
- Hybrid approach: Repository pattern with explicit SQL
- Connection pooling and transaction management

### EF Core Migration Goals

**Target Performance:**

- Maintain or exceed current performance benchmarks
- Eliminate N+1 query problems
- Optimize change tracking and memory usage
- Minimize database round trips

**Migration Strategy:**

- **Gradual Migration**: Convert repositories incrementally
- **Hybrid Approach**: Use EF Core with raw SQL where beneficial
- **Performance-First**: Optimize for high-throughput scenarios

---

## Migration Prerequisites

### Development Environment

- **EF Core Tools**: `dotnet tool install --global dotnet-ef`
- **Database**: PostgreSQL (current) or SQL Server (after migration)
- **Monitoring**: Query performance analysis tools

### Package Dependencies

**Required EF Core Packages:**

```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="10.0.2" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="10.0.0" />
<!-- OR for SQL Server -->
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="10.0.2" />

<!-- Performance and Monitoring (TBD) -->
<PackageReference Include="Microsoft.EntityFrameworkCore.Proxies" Version="10.0.2" />
<PackageReference Include="EFCore.NamingConventions" Version="10.0.1" />
<PackageReference Include="EntityFrameworkCore.Exceptions.PostgreSQL" Version="8.1.3" />
```

### Database Considerations

**Current Schema:**

- PostgreSQL with `webshop` schema
- Explicit foreign key relationships
- Manual audit field management
- Soft delete via `isactive` column

---

## EF Core Setup and Configuration

### 1. DbContext Configuration

**Use Separate Read/Write DbContexts for optimal performance:**

```csharp
// Read-optimized context (similar to Dapper's read connection)
public class ApplicationReadDbContext : DbContext
{
    public ApplicationReadDbContext(DbContextOptions<ApplicationReadDbContext> options)
        : base(options) { }

    // DbSets for all entities
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    // ... other entities

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationReadDbContext).Assembly);
        // No global query filters for read context (can query soft-deleted records if needed)
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Read-optimized settings
            optionsBuilder
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) // Always no-tracking for reads
                .EnableSensitiveDataLogging(false)
                .EnableDetailedErrors(false);
        }
    }
}

// Write-optimized context (similar to Dapper's write connection)
public class ApplicationWriteDbContext : DbContext
{
    public ApplicationWriteDbContext(DbContextOptions<ApplicationWriteDbContext> options)
        : base(options) { }

    // DbSets for all entities
    public DbSet<Customer> Customers { get; set; } = null!;
    public DbSet<Order> Orders { get; set; } = null!;
    public DbSet<Product> Products { get; set; } = null!;
    // ... other entities

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationWriteDbContext).Assembly);

        // Global query filters for soft delete (only applied to write operations)
        modelBuilder.Entity<Customer>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<Order>().HasQueryFilter(e => e.IsActive);
        modelBuilder.Entity<Product>().HasQueryFilter(e => e.IsActive);
        // ... apply to all entities with soft delete
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Write-optimized settings
            optionsBuilder
                .UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll) // Change tracking for writes
                .EnableSensitiveDataLogging(false)
                .EnableDetailedErrors(false)
                .UseLazyLoadingProxies(); // Enable lazy loading for write operations
        }
    }
}
```

### 2. Entity Configurations

**Customer Entity Configuration:**

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebShop.Core.Entities;

namespace WebShop.Infrastructure.Data.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("customer", "webshop");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .UseIdentityColumn() // SERIAL equivalent
            .HasColumnName("id");

        builder.Property(c => c.FirstName)
            .HasColumnName("firstname")
            .HasMaxLength(100);

        builder.Property(c => c.LastName)
            .HasColumnName("lastname")
            .HasMaxLength(100);

        builder.Property(c => c.Email)
            .HasColumnName("email")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .HasColumnName("created")
            .HasDefaultValueSql("CURRENT_TIMESTAMP")
            .ValueGeneratedOnAdd();

        builder.Property(c => c.IsActive)
            .HasColumnName("isactive")
            .HasDefaultValue(true);

        // Indexes for performance
        builder.HasIndex(c => c.Email).IsUnique().HasFilter("isactive = true");
        builder.HasIndex(c => new { c.FirstName, c.LastName });

        // Relationships
        builder.HasOne<Address>()
            .WithMany()
            .HasForeignKey(c => c.CurrentAddressId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
```

### 3. Dependency Injection Setup

**Register Separate Read/Write DbContexts:**

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    var connectionSettings = configuration.GetSection("DbConnectionSettings").Get<DbConnectionModel>()
        ?? configuration.GetSection("DatabaseConnectionSettings").Get<DbConnectionModel>()
        ?? throw new InvalidOperationException("Database connection settings not found");

    string globalApplicationName = configuration.GetValue<string>("AppSettings:ApplicationName") ?? "WebShop.Api";

    // Read context (optimized for queries, uses read connection)
    services.AddDbContext<ApplicationReadDbContext>(options =>
    {
        string connectionString = DbConnectionModel.CreateConnectionString(connectionSettings.Read, globalApplicationName);
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(1), null);
            npgsqlOptions.CommandTimeout(30);
        });
    });

    // Write context (optimized for changes, uses write connection)
    services.AddDbContext<ApplicationWriteDbContext>(options =>
    {
        string connectionString = DbConnectionModel.CreateConnectionString(connectionSettings.Write, globalApplicationName);
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(1), null);
            npgsqlOptions.CommandTimeout(60); // Longer timeout for write operations
            npgsqlOptions.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
        });
    });

    // Register repositories with both contexts
    services.AddScoped<ICustomerRepository, EfCustomerRepository>();
    // ... other repositories

    return services;
}
```

---

## Performance Optimization Strategies

### 1. Change Tracking Optimization

**Problem**: EF Core tracks all entities by default, causing memory overhead.

**Solutions:**

```csharp
// 1. Use AsNoTracking() for read-only queries
public async Task<Customer?> GetByIdAsync(int id)
{
    return await _context.Customers
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == id);
}

// 2. Use AsNoTrackingWithIdentityResolution() for graphs
public async Task<Customer> GetWithOrdersAsync(int customerId)
{
    return await _context.Customers
        .AsNoTrackingWithIdentityResolution()
        .Include(c => c.Orders)
        .FirstOrDefaultAsync(c => c.Id == customerId);
}

// 3. Disable change tracking at context level (configured above)
```

### 2. N+1 Query Prevention

**Problem**: Lazy loading causes N+1 queries.

**Solutions:**

```csharp
// ❌ BAD: Causes N+1 queries
public async Task<List<Customer>> GetCustomersWithOrders()
{
    var customers = await _context.Customers.ToListAsync(); // 1 query
    foreach (var customer in customers)
    {
        await _context.Entry(customer).Reference(c => c.Orders).LoadAsync(); // N queries
    }
    return customers;
}

// ✅ GOOD: Use Include() for eager loading
public async Task<List<Customer>> GetCustomersWithOrders()
{
    return await _context.Customers
        .Include(c => c.Orders) // Single query with JOIN
        .ToListAsync();
}

// ✅ BETTER: Use SplitQuery for complex joins
public async Task<List<Customer>> GetCustomersWithOrdersAndItems()
{
    return await _context.Customers
        .Include(c => c.Orders)
            .ThenInclude(o => o.OrderPositions)
                .ThenInclude(op => op.Product)
        .AsSplitQuery() // Split into multiple queries to avoid Cartesian explosion
        .ToListAsync();
}
```

### 3. Cartesian Explosion Prevention

**Problem**: Multiple `Include()` calls can create massive result sets.

**Solutions:**

```csharp
// ❌ BAD: Cartesian explosion with multiple includes
public async Task<Customer> GetCustomerWithAllData(int id)
{
    return await _context.Customers
        .Include(c => c.Orders)
            .ThenInclude(o => o.OrderPositions)
        .Include(c => c.Addresses)
        .Include(c => c.Preferences)
        .FirstOrDefaultAsync(c => c.Id == id); // Massive Cartesian product
}

// ✅ GOOD: Use SplitQuery to avoid Cartesian explosion
public async Task<Customer> GetCustomerWithAllData(int id)
{
    return await _context.Customers
        .Include(c => c.Orders)
            .ThenInclude(o => o.OrderPositions)
        .Include(c => c.Addresses)
        .Include(c => c.Preferences)
        .AsSplitQuery() // Split into separate queries
        .FirstOrDefaultAsync(c => c.Id == id);
}

// ✅ BETTER: Use explicit queries for complex scenarios
public async Task<CustomerDetailsDto> GetCustomerDetailsAsync(int customerId)
{
    // Use multiple targeted queries instead of one massive join
    var customer = await _context.Customers
        .AsNoTracking()
        .FirstOrDefaultAsync(c => c.Id == customerId);

    if (customer == null) return null;

    var orders = await _context.Orders
        .AsNoTracking()
        .Where(o => o.CustomerId == customerId)
        .Select(o => new OrderSummary { /* projection */ })
        .ToListAsync();

    var addresses = await _context.Addresses
        .AsNoTracking()
        .Where(a => a.CustomerId == customerId)
        .ToListAsync();

    return new CustomerDetailsDto
    {
        Customer = customer,
        Orders = orders,
        Addresses = addresses
    };
}
```

### 4. Query Optimization Techniques

**Projection for Better Performance:**

```csharp
// ❌ BAD: Select entire entities when only need some fields
public async Task<List<CustomerSummary>> GetCustomerSummaries()
{
    return await _context.Customers
        .Select(c => new CustomerSummary
        {
            Id = c.Id,
            FullName = c.FirstName + " " + c.LastName,
            Email = c.Email,
            OrderCount = c.Orders.Count // This causes N+1!
        })
        .ToListAsync();
}

// ✅ GOOD: Use aggregation in SQL
public async Task<List<CustomerSummary>> GetCustomerSummaries()
{
    return await _context.Customers
        .Select(c => new CustomerSummary
        {
            Id = c.Id,
            FullName = c.FirstName + " " + c.LastName,
            Email = c.Email,
            OrderCount = c.Orders.Count(o => o.IsActive) // Aggregation in SQL
        })
        .ToListAsync();
}

// ✅ BETTER: Use separate query for complex aggregations
public async Task<List<CustomerSummary>> GetCustomerSummaries()
{
    var customerIds = await _context.Customers
        .Select(c => c.Id)
        .ToListAsync();

    var orderCounts = await _context.Orders
        .Where(o => customerIds.Contains(o.CustomerId) && o.IsActive)
        .GroupBy(o => o.CustomerId)
        .Select(g => new { CustomerId = g.Key, Count = g.Count() })
        .ToDictionaryAsync(x => x.CustomerId, x => x.Count);

    return await _context.Customers
        .Select(c => new CustomerSummary
        {
            Id = c.Id,
            FullName = c.FirstName + " " + c.LastName,
            Email = c.Email,
            OrderCount = orderCounts.GetValueOrDefault(c.Id, 0)
        })
        .ToListAsync();
}
```

### 5. Connection and Transaction Management

**EF Core Repository with Separate Read/Write Contexts:**

```csharp
public class CustomerRepository : ICustomerRepository
{
    private readonly ApplicationReadDbContext _readContext;
    private readonly ApplicationWriteDbContext _writeContext;
    private readonly ILogger<CustomerRepository> _logger;

    public CustomerRepository(
        ApplicationReadDbContext readContext,
        ApplicationWriteDbContext writeContext,
        ILogger<CustomerRepository> logger)
    {
        _readContext = readContext;
        _writeContext = writeContext;
        _logger = logger;
    }

    // Read operations use read-optimized context (always NoTracking)
    public async Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _readContext.Customers
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var customers = await _readContext.Customers
            .ToListAsync(cancellationToken);

        return customers;
    }

    // Write operations use write context with change tracking
    public async Task<Customer> AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _writeContext.Customers.AddAsync(customer, cancellationToken);
        await _writeContext.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _writeContext.Customers.Update(customer);
        await _writeContext.SaveChangesAsync(cancellationToken);
    }

    // Complex operations with transactions
    public async Task<bool> TransferCustomerAsync(int fromCustomerId, int toCustomerId, CancellationToken cancellationToken = default)
    {
        using var transaction = await _writeContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var fromCustomer = await _writeContext.Customers.FindAsync(fromCustomerId);
            var toCustomer = await _writeContext.Customers.FindAsync(toCustomerId);

            if (fromCustomer == null || toCustomer == null)
                return false;

            // Complex business logic here...
            // Example: Transfer orders from one customer to another

            await _writeContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
```

---

## Security Best Practices

### 1. SQL Injection Prevention

**EF Core automatically parameterizes queries:**

```csharp
// ✅ SAFE: EF Core automatically parameterizes
public async Task<Customer?> GetByEmailAsync(string email)
{
    return await _context.Customers
        .FirstOrDefaultAsync(c => c.Email == email);
}

// ❌ DANGEROUS: Raw SQL without parameters (avoid if possible)
public async Task<Customer?> GetByEmailUnsafeAsync(string email)
{
    return await _context.Customers
        .FromSqlRaw($"SELECT * FROM customers WHERE email = '{email}'") // SQL Injection!
        .FirstOrDefaultAsync();
}

// ✅ SAFE: Raw SQL with parameters
public async Task<Customer?> GetByEmailSafeAsync(string email)
{
    return await _context.Customers
        .FromSqlInterpolated($"SELECT * FROM customers WHERE email = {email}")
        .FirstOrDefaultAsync();
}
```

### 2. Sensitive Data Protection

**Never expose sensitive data in logs:**

```csharp
// Configure in Program.cs or Startup.cs
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString)
        .EnableSensitiveDataLogging(false) // Never log sensitive data
        .EnableDetailedErrors(false); // Don't expose internal errors in production
});
```

### 3. Input Validation and Sanitization

**Use EF Core validation and FluentValidation:**

```csharp
public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.Property(c => c.Email)
            .HasMaxLength(255)
            .IsRequired();

        // Add check constraints for data integrity
        builder.ToTable(tb => tb.HasCheckConstraint("CK_Customer_Email", "Email LIKE '%@%'"));
    }
}

// Additional validation with FluentValidation
public class CustomerValidator : AbstractValidator<Customer>
{
    public CustomerValidator()
    {
        RuleFor(c => c.Email)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(255);

        RuleFor(c => c.FirstName)
            .MaximumLength(100)
            .Matches(@"^[a-zA-Z\s\-']+$").WithMessage("First name contains invalid characters");
    }
}
```

### 4. Authorization and Row-Level Security

**Implement proper authorization:**

```csharp
public async Task<List<Order>> GetCustomerOrdersAsync(int customerId, int currentUserId)
{
    // Ensure user can only see their own orders
    return await _context.Orders
        .Where(o => o.CustomerId == customerId && o.CustomerId == currentUserId)
        .ToListAsync();
}

// Or use global query filters for multi-tenant scenarios
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>().HasQueryFilter(o => o.Customer.UserId == _currentUserId);
}
```

---

## Bulk Operations in EF Core

### The Performance Challenge

**EF Core's default bulk operations are slow** because:

- Each entity is tracked individually
- Change detection runs for every entity
- Multiple database round trips for large datasets
- Memory overhead from change tracking

**Performance Comparison:**

```csharp
// ❌ SLOW: Default EF Core approach
foreach (var customer in customers)
{
    _writeContext.Customers.Add(customer);
}
await _writeContext.SaveChangesAsync(); // Very slow for 1000+ entities

// ✅ FAST: Bulk insert libraries
await _writeContext.BulkInsertAsync(customers); // 10-50x faster
```

### Recommended Bulk Operation Libraries

**1. EFCore.BulkExtensions (Most Popular):**

```xml
<PackageReference Include="EFCore.BulkExtensions" Version="8.0.4" />
```

**2. EntityFrameworkCore.BulkExtensions:**

```xml
<PackageReference Include="EntityFrameworkCore.BulkExtensions" Version="8.0.2" />
```

**3. Z.EntityFramework.Plus.EFCore (for .NET Core):**

```xml
<PackageReference Include="Z.EntityFramework.Plus.EFCore" Version="8.102.0.1" />
```

### Bulk Insert Examples

**Using EFCore.BulkExtensions:**

```csharp
using EFCore.BulkExtensions;

// Bulk insert with high performance
public async Task BulkInsertCustomersAsync(IEnumerable<Customer> customers)
{
    await using var transaction = await _writeContext.Database.BeginTransactionAsync();

    try
    {
        // Set audit fields for all entities
        var customersList = customers.ToList();
        foreach (var customer in customersList)
        {
            SetAuditFieldsForCreate(customer);
        }

        // Bulk insert (10-50x faster than SaveChanges)
        await _writeContext.BulkInsertAsync(customersList, new BulkConfig
        {
            BatchSize = 1000,           // Process in batches
            BulkCopyTimeout = 300,      // 5 minute timeout
            EnableStreaming = true,     // Reduce memory usage
            SetOutputIdentity = true    // Set identity values back to entities
        });

        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

**Advanced Bulk Insert with Custom Mapping:**

```csharp
public async Task BulkInsertOrdersAsync(IEnumerable<Order> orders)
{
    var bulkConfig = new BulkConfig
    {
        // Performance optimizations
        BatchSize = 2000,
        BulkCopyTimeout = 600,
        EnableStreaming = true,
        SetOutputIdentity = true,

        // Custom column mapping if needed
        PropertiesToInclude = new List<string>
        {
            nameof(Order.Id),
            nameof(Order.CustomerId),
            nameof(Order.TotalAmount),
            nameof(Order.CreatedAt),
            nameof(Order.CreatedBy)
        },

        // Handle conflicts (PostgreSQL/SQL Server specific)
        OnConflictUpdateWhereSql = "WHERE \"isactive\" = true"
    };

    await _writeContext.BulkInsertAsync(orders, bulkConfig);
}
```

### Bulk Update Examples

**Bulk Update with Conditions:**

```csharp
public async Task BulkUpdateCustomerStatusAsync(IEnumerable<int> customerIds, bool isActive)
{
    // Method 1: Using BulkExtensions
    await _writeContext.Customers
        .Where(c => customerIds.Contains(c.Id))
        .BatchUpdateAsync(c => new Customer
        {
            IsActive = isActive,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = _currentUserId
        });

    // Method 2: Direct SQL for maximum performance
    var idsList = string.Join(",", customerIds);
    var sql = $@"
        UPDATE ""webshop"".""customer""
        SET ""isactive"" = @IsActive,
            ""updated"" = @UpdatedAt,
            ""updatedby"" = @UpdatedBy
        WHERE ""id"" = ANY(@Ids)";

    await _writeContext.Database.ExecuteSqlRawAsync(sql,
        new NpgsqlParameter("@IsActive", isActive),
        new NpgsqlParameter("@UpdatedAt", DateTime.UtcNow),
        new NpgsqlParameter("@Ids", customerIds.ToArray()));
}
```

### Bulk Delete Examples

**Soft Delete Bulk Operation:**

```csharp
public async Task BulkSoftDeleteCustomersAsync(IEnumerable<int> customerIds)
{
    // Method 1: Bulk update for soft delete
    await _writeContext.Customers
        .Where(c => customerIds.Contains(c.Id) && c.IsActive)
        .BatchUpdateAsync(c => new Customer
        {
            IsActive = false,
            UpdatedAt = DateTime.UtcNow,
            UpdatedBy = _currentUserId
        });

    // Method 2: Direct SQL for complex bulk operations
    var sql = $@"
        UPDATE ""webshop"".""customer""
        SET ""isactive"" = false,
            ""updated"" = @UpdatedAt,
            ""updatedby"" = @UpdatedBy
        WHERE ""id"" = ANY(@Ids) AND ""isactive"" = true";

    await _writeContext.Database.ExecuteSqlRawAsync(sql,
        new NpgsqlParameter("@UpdatedAt", DateTime.UtcNow),
        new NpgsqlParameter("@Ids", customerIds.ToArray()));
}
```

### Performance Optimization Strategies

**1. Batch Size Optimization:**

```csharp
// Test different batch sizes for your scenario
var bulkConfig = new BulkConfig
{
    BatchSize = 500,        // Smaller batches for memory-constrained environments
    // BatchSize = 2000,    // Larger batches for high-performance scenarios
    // BatchSize = 10000,   // Very large batches for bulk data operations
};
```

**2. Memory Management:**

```csharp
// For large datasets, use streaming
var bulkConfig = new BulkConfig
{
    EnableStreaming = true,              // Reduce memory usage
    TrackingEntities = false,            // Don't track entities
    UseTempDB = true,                   // Use temp database for large operations
};
```

**3. Transaction Management:**

```csharp
// Use appropriate transaction isolation for bulk operations
using var transaction = await _writeContext.Database.BeginTransactionAsync(
    IsolationLevel.ReadCommitted); // or ReadUncommitted for performance

try
{
    await _writeContext.BulkInsertAsync(entities, bulkConfig);
    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}
```

### When to Use Bulk Operations

**Use Bulk Operations When:**

- ✅ Inserting 100+ entities
- ✅ Updating 50+ entities with the same changes
- ✅ Deleting 50+ entities
- ✅ Processing large datasets
- ✅ ETL operations
- ✅ Data import/export scenarios

**Use Regular EF Core When:**

- ❌ Simple CRUD operations (< 50 entities)
- ❌ Complex business logic per entity
- ❌ Validation logic per entity
- ❌ Entity relationships need to be maintained
- ❌ Change tracking is required for further operations

### Performance Benchmarks

| Operation | EF Core Default | Bulk Extensions | Improvement |
|-----------|----------------|----------------|-------------|
| **Insert 1,000 rows** | ~5-10 seconds | ~0.2-0.5 seconds | 20-50x faster |
| **Update 500 rows** | ~3-5 seconds | ~0.1-0.3 seconds | 15-30x faster |
| **Delete 200 rows** | ~1-2 seconds | ~0.05-0.1 seconds | 10-20x faster |
| **Memory Usage** | High (tracking) | Low (streaming) | 5-10x less memory |

### Best Practices

**1. Choose the Right Tool:**

```csharp
// Use EFCore.BulkExtensions for general bulk operations
// Use direct SQL for maximum performance with complex logic
// Use regular EF Core for small datasets with business logic
```

**2. Handle Transactions Carefully:**

```csharp
// Bulk operations work best with explicit transactions
// Consider transaction isolation levels for performance vs consistency
// Test rollback scenarios thoroughly
```

**3. Monitor Performance:**

```csharp
// Log bulk operation metrics
_logger.LogInformation("Bulk inserted {Count} customers in {Elapsed}ms",
    customers.Count, stopwatch.ElapsedMilliseconds);

// Monitor database performance during bulk operations
```

**4. Error Handling:**

```csharp
try
{
    await _writeContext.BulkInsertAsync(entities, bulkConfig);
}
catch (BulkException ex)
{
    // Handle bulk operation specific errors
    _logger.LogError(ex, "Bulk operation failed: {Message}", ex.Message);
    // Log failed entities if available
    foreach (var failedEntity in ex.FailedEntities)
    {
        _logger.LogError("Failed to process entity: {Entity}", failedEntity);
    }
}
```

**5. Test Thoroughly:**

```csharp
// Test with production-like data volumes
// Verify data integrity after bulk operations
// Test error scenarios and rollback behavior
// Monitor database performance and blocking
```

---

## Code Migration Process

### Phase 1: Infrastructure Setup

1. **Add EF Core packages** to project files
2. **Create DbContext** and entity configurations
3. **Update dependency injection** to register DbContext

### Phase 2: Repository Migration

**Convert one repository at a time:**

```csharp
// Before: Dapper-based repository
public class CustomerRepository : DapperRepositoryBase<Customer>, ICustomerRepository
{
    public async Task<Customer?> GetByIdAsync(int id)
    {
        const string sql = @"SELECT ... FROM customer WHERE id = @Id";
        using var connection = GetReadConnection();
        return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { Id = id });
    }
}

// After: EF Core-based repository
public class EfCustomerRepository : ICustomerRepository
{
    private readonly ApplicationReadDbContext _readContext;
    private readonly ApplicationWriteDbContext _writeContext;

    public EfCustomerRepository(ApplicationReadDbContext readContext, ApplicationWriteDbContext writeContext)
    {
        _readContext = readContext;
        _writeContext = writeContext;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _readContext.Customers
            .FirstOrDefaultAsync(c => c.Id == id);
    }
}
```

### Phase 3: Interface Updates

**Update repository interfaces to match EF Core patterns:**

```csharp
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    // EF Core specific methods
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

### Phase 4: Service Layer Updates

**Update services to work with EF Core change tracking:**

```csharp
public class CustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork; // EF Core equivalent

    public async Task<Customer> CreateCustomerAsync(CreateCustomerRequest request)
    {
        var customer = new Customer
        {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email
        };

        // Set audit fields
        SetAuditFields(customer, AuditOperation.Create);

        await _customerRepository.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync(); // EF Core handles transaction

        return customer;
    }
}
```

---

## Common EF Core Issues and Solutions

### 1. N+1 Query Problem

**Detection:**

```sql
-- Look for multiple SELECT statements in logs
SELECT * FROM Customers WHERE IsActive = 1
SELECT * FROM Orders WHERE CustomerId = 1
SELECT * FROM Orders WHERE CustomerId = 2
-- ... N more queries
```

**Solutions:**

- Use `Include()` for eager loading
- Use `AsSplitQuery()` to avoid Cartesian explosion
- Use projection to select only needed data
- Consider denormalization for read-heavy scenarios

### 2. Memory Issues with Large Datasets

**Problem:** Loading thousands of entities consumes excessive memory.

**Solutions:**

```csharp
// Use streaming for large datasets
public async IAsyncEnumerable<Customer> GetAllCustomersStream()
{
    await foreach (var customer in _context.Customers
        .AsNoTracking()
        .AsAsyncEnumerable())
    {
        yield return customer;
    }
}

// Use pagination for large lists
public async Task<List<Customer>> GetCustomersPaged(int page, int size)
{
    return await _context.Customers
        .AsNoTracking()
        .Skip((page - 1) * size)
        .Take(size)
        .ToListAsync();
}

// Use projection to reduce memory footprint
public async Task<List<CustomerSummary>> GetCustomerSummaries()
{
    return await _context.Customers
        .AsNoTracking()
        .Select(c => new CustomerSummary
        {
            Id = c.Id,
            Name = c.FirstName + " " + c.LastName,
            OrderCount = c.Orders.Count
        })
        .ToListAsync();
}
```

### 3. Slow First Query (Cold Start)

**Problem:** EF Core compiles queries on first execution.

**Solutions:**

```csharp
// Pre-compile frequently used queries
private static readonly Func<ApplicationDbContext, int, Task<Customer?>> _getCustomerByIdCompiled =
    EF.CompileAsyncQuery((ApplicationDbContext context, int id) =>
        context.Customers.FirstOrDefault(c => c.Id == id));

public async Task<Customer?> GetByIdAsync(int id)
{
    return await _getCustomerByIdCompiled(_context, id);
}

// Warm up the context on application start
public static async Task WarmupAsync(ApplicationDbContext context)
{
    await context.Customers.FirstOrDefaultAsync(c => c.Id == -1);
}
```

### 4. Transaction Issues

**Problem:** EF Core transactions can be complex with multiple contexts.

**Solutions:**

```csharp
// Use single DbContext per request (scoped lifetime)
services.AddDbContext<ApplicationDbContext>(options => ..., ServiceLifetime.Scoped);

// For distributed transactions across multiple contexts
public async Task TransferDataAsync()
{
    using var transaction = await _context.Database.BeginTransactionAsync();

    try
    {
        // Multiple operations
        await _context.SaveChangesAsync();
        await _otherContext.SaveChangesAsync();

        await transaction.CommitAsync();
    }
    catch
    {
        await transaction.RollbackAsync();
        throw;
    }
}
```

### 5. Concurrency Conflicts

**Problem:** Multiple users updating the same data.

**Solutions:**

```csharp
// Optimistic concurrency with RowVersion
public class Customer : BaseEntity
{
    [Timestamp] // SQL Server
    [ConcurrencyCheck] // PostgreSQL
    public byte[] RowVersion { get; set; } = null!;
}

// Handle concurrency exceptions
public async Task UpdateCustomerAsync(Customer customer)
{
    try
    {
        _context.Customers.Update(customer);
        await _context.SaveChangesAsync();
    }
    catch (DbUpdateConcurrencyException ex)
    {
        // Handle concurrency conflict
        var entry = ex.Entries.Single();
        var databaseValues = await entry.GetDatabaseValuesAsync();

        if (databaseValues == null)
        {
            throw new NotFoundException("Customer was deleted by another user");
        }

        var databaseCustomer = (Customer)databaseValues.ToObject();
        throw new ConcurrencyException(databaseCustomer);
    }
}
```

---

## Testing and Validation

### Unit Testing with EF Core

**Use In-Memory Database for Unit Tests:**

```csharp
public class CustomerRepositoryTests
{
    private ApplicationDbContext _context;
    private EfCustomerRepository _repository;

    [SetUp]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new EfCustomerRepository(_context);

        // Seed test data
        _context.Customers.Add(new Customer { Id = 1, Email = "test@example.com" });
        _context.SaveChanges();
    }

    [Test]
    public async Task GetByIdAsync_ReturnsCustomer_WhenExists()
    {
        var result = await _repository.GetByIdAsync(1);
        Assert.IsNotNull(result);
        Assert.AreEqual("test@example.com", result.Email);
    }
}
```

### Integration Testing

**Test with Real Database:**

```csharp
[TestFixture]
public class CustomerRepositoryIntegrationTests : DatabaseTestBase
{
    [Test]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        // Arrange - seed database with test data
        await SeedCustomersAsync(100);

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(2, 10);

        // Assert
        Assert.AreEqual(10, items.Count);
        Assert.AreEqual(100, totalCount);
    }
}
```

### Performance Testing

**Benchmark EF Core vs Dapper:**

```csharp
[Benchmark]
public async Task EfCore_GetAllCustomers()
{
    var customers = await _efRepository.GetAllAsync();
}

[Benchmark]
public async Task Dapper_GetAllCustomers()
{
    var customers = await _dapperRepository.GetAllAsync();
}
```

---

## Performance Comparison

### Benchmark Results (Estimated)

| Operation | Dapper | EF Core (Optimized) | EF Core (Default) |
|-----------|--------|-------------------|------------------|
| **Simple SELECT** | 1.0x | 1.1x | 2.0x |
| **Complex JOIN** | 1.0x | 0.9x | 1.8x |
| **INSERT** | 1.0x | 1.2x | 2.5x |
| **UPDATE** | 1.0x | 1.1x | 2.2x |
| **Memory Usage** | 1.0x | 1.0x | 1.5x |

### Key Findings

**EF Core Advantages:**

- Better query optimization for complex scenarios
- Automatic SQL generation reduces bugs
- Rich LINQ support for complex queries
- Built-in change tracking for complex business logic

**Dapper Advantages:**

- Minimal overhead for simple operations
- Full control over SQL generation
- Predictable performance characteristics
- Smaller memory footprint

**Migration Recommendation:**

- Use EF Core for complex business logic with multiple entity relationships
- Use Dapper (raw SQL) for high-volume, simple CRUD operations
- Consider hybrid approach: EF Core for complex queries, Dapper for simple reads

---

## Summary

### Migration Benefits

✅ **Developer Productivity**: Rich LINQ queries and automatic SQL generation  
✅ **Type Safety**: Compile-time query validation  
✅ **Relationship Management**: Automatic handling of foreign keys and navigation  
✅ **Change Tracking**: Automatic dirty checking and optimistic concurrency  
✅ **Migration Support**: Built-in database schema migrations  

### Performance Considerations

⚠️ **ORM Overhead**: EF Core has higher baseline overhead than Dapper  
⚠️ **N+1 Queries**: Requires careful use of `Include()` and `AsSplitQuery()`  
⚠️ **Memory Usage**: Change tracking can consume more memory  
⚠️ **Cold Starts**: Query compilation on first execution
