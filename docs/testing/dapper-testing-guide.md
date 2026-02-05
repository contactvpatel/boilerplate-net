# Dapper Repository Testing Guide

[← Back to README](../README.md)

## Table of Contents

- [Overview](#overview)
- [Test Approach](#test-approach)
- [Test Structure](#test-structure)
- [Important Notes](#important-notes)
- [Testing Custom Repository Methods](#testing-custom-repository-methods)
- [Testing Error Cases](#testing-error-cases)
- [Common Patterns](#common-patterns)
- [Best Practices](#best-practices)
- [Running Tests](#running-tests)
- [Troubleshooting](#troubleshooting)
- [Summary](#summary)

---

## Overview

This guide explains how to write unit tests for Dapper repositories using mocked database connections. Since we're using Dapper (not Dapper), we mock `IDbConnection` and configure return values for queries.

## Test Approach

### Mock-Based Testing (Current Approach)

We use **Moq** to mock database connections and configure expected return values. This provides:
- **Fast execution** - No database I/O
- **Isolation** - Each test is independent
- **Simplicity** - No database setup/teardown
- **Focus** - Tests verify repository logic, not SQL correctness

### Why Not Integration Tests?

For Dapper repositories, we focus on **unit tests** that verify:
- Correct SQL query construction
- Proper parameter binding
- Result mapping from dynamic to entity types
- Error handling

**Integration tests** with a real PostgreSQL database should be done at the service or API level, not at the repository level.

## Test Structure

### 1. Test Class Setup

```csharp
public class CustomerRepositoryTests : IDisposable
{
    private readonly DapperTestDatabase _testDatabase;
    private readonly CustomerRepository _repository;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;

    public CustomerRepositoryTests()
    {
        _testDatabase = new DapperTestDatabase();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _repository = new CustomerRepository(
            _testDatabase.ConnectionFactory,
            _testDatabase.TransactionManager,
            _mockLoggerFactory.Object);
    }

    public void Dispose()
    {
        _testDatabase?.Dispose();
    }
}
```

### 2. Writing Test Methods

Each test method should:
1. **Arrange** - Set up mock data using `Dictionary<string, object>`
2. **Act** - Call the repository method
3. **Assert** - Verify the result

#### Example: Testing GetByIdAsync

```csharp
[Fact]
public async Task GetByIdAsync_ValidId_ReturnsCustomer()
{
    // Arrange
    var mockCustomer = new Dictionary<string, object>
    {
        { "id", 1 },
        { "firstname", "John" },
        { "lastname", "Doe" },
        { "email", "john.doe@example.com" },
        { "isactive", true },
        { "created", DateTime.UtcNow },
        { "createdby", 1 },
        { "updatedby", 1 }
    };
    _testDatabase.SetupQueryFirstOrDefault(mockCustomer);

    // Act
    Customer? result = await _repository.GetByIdAsync(1);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(1);
    result.FirstName.Should().Be("John");
}
```

#### Example: Testing GetAllAsync

```csharp
[Fact]
public async Task GetAllAsync_ReturnsAllActiveCustomers()
{
    // Arrange
    var customers = new[]
    {
        new Dictionary<string, object>
        {
            { "id", 1 },
            { "firstname", "John" },
            { "lastname", "Doe" },
            { "email", "john.doe@example.com" },
            { "isactive", true },
            { "created", DateTime.UtcNow },
            { "createdby", 1 },
            { "updatedby", 1 }
        },
        new Dictionary<string, object>
        {
            { "id", 2 },
            { "firstname", "Jane" },
            { "lastname", "Smith" },
            { "email", "jane.smith@example.com" },
            { "isactive", true },
            { "created", DateTime.UtcNow },
            { "createdby", 1 },
            { "updatedby", 1 }
        }
    };
    _testDatabase.SetupQuery(customers);

    // Act
    IReadOnlyList<Customer> result = await _repository.GetAllAsync();

    // Assert
    result.Should().NotBeNull();
    result.Should().HaveCount(2);
}
```

### 3. Helper Methods in DapperTestDatabase

The `DapperTestDatabase` helper provides these methods:

- **`SetupQuery(IEnumerable<Dictionary<string, object>>)`** - For `QueryAsync` (returns multiple rows)
- **`SetupQueryFirstOrDefault(Dictionary<string, object>?)`** - For `QueryFirstOrDefaultAsync` (returns single row or null)
- **`SetupScalar(bool)`** - For EXISTS queries
- **`SetupScalar(int)`** - For INSERT queries that return IDs
- **`SetupExecute(int)`** - For UPDATE/DELETE queries (returns rows affected)

## Important Notes

### Column Naming

Use **lowercase** column names in mock dictionaries to match PostgreSQL conventions:

```csharp
// ✅ Correct
{ "firstname", "John" }

// ❌ Incorrect
{ "FirstName", "John" }
```

### Null Values

For null values, use `DBNull.Value` or omit the key:

```csharp
// Option 1: Omit the key
var mockData = new Dictionary<string, object>
{
    { "id", 1 },
    { "firstname", "John" }
    // lastname is null (not included)
};

// Option 2: Use DBNull.Value
var mockData = new Dictionary<string, object>
{
    { "id", 1 },
    { "firstname", "John" },
    { "lastname", DBNull.Value }
};
```

### DateTime Values

Use `DateTime.UtcNow` for timestamp fields:

```csharp
{ "created", DateTime.UtcNow }
```

### Boolean Values

Use `true`/`false` (not 1/0):

```csharp
{ "isactive", true }
```

## Testing Custom Repository Methods

For custom repository methods (e.g., `GetByEmailAsync`), follow the same pattern:

```csharp
[Fact]
public async Task GetByEmailAsync_ValidEmail_ReturnsCustomer()
{
    // Arrange
    const string email = "john.doe@example.com";
    var mockCustomer = new Dictionary<string, object>
    {
        { "id", 1 },
        { "firstname", "John" },
        { "email", email },
        { "isactive", true },
        { "created", DateTime.UtcNow },
        { "createdby", 1 },
        { "updatedby", 1 }
    };
    _testDatabase.SetupQueryFirstOrDefault(mockCustomer);

    // Act
    Customer? result = await _repository.GetByEmailAsync(email);

    // Assert
    result.Should().NotBeNull();
    result!.Email.Should().Be(email);
}
```

## Testing Error Cases

Test null returns and empty collections:

```csharp
[Fact]
public async Task GetByIdAsync_InvalidId_ReturnsNull()
{
    // Arrange
    _testDatabase.SetupQueryFirstOrDefault(null);

    // Act
    Customer? result = await _repository.GetByIdAsync(999);

    // Assert
    result.Should().BeNull();
}

[Fact]
public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
{
    // Arrange
    _testDatabase.SetupQuery(Array.Empty<Dictionary<string, object>>());

    // Act
    IReadOnlyList<Customer> result = await _repository.GetAllAsync();

    // Assert
    result.Should().NotBeNull();
    result.Should().BeEmpty();
}
```

## Common Patterns

### Testing Pagination

```csharp
[Fact]
public async Task GetPagedAsync_ReturnsPagedResults()
{
    // Arrange
    var customers = new[]
    {
        new Dictionary<string, object>
        {
            { "id", 1 },
            { "firstname", "John" },
            { "isactive", true },
            { "created", DateTime.UtcNow },
            { "createdby", 1 },
            { "updatedby", 1 },
            { "TotalCount", 100 } // Window function result
        }
    };
    _testDatabase.SetupQuery(customers);

    // Act
    var (items, totalCount) = await _repository.GetPagedAsync(1, 10);

    // Assert
    items.Should().HaveCount(1);
    totalCount.Should().Be(100);
}
```

### Testing Soft Deletes

```csharp
[Fact]
public async Task GetByIdAsync_SoftDeleted_ReturnsNull()
{
    // Arrange - Repository filters by IsActive = true
    _testDatabase.SetupQueryFirstOrDefault(null);

    // Act
    Customer? result = await _repository.GetByIdAsync(3);

    // Assert
    result.Should().BeNull(); // Soft-deleted entities are filtered out
}
```

## Best Practices

1. **One assertion per test** - Focus each test on a single behavior
2. **Use descriptive test names** - Follow the pattern `MethodName_Scenario_ExpectedResult`
3. **Arrange-Act-Assert** - Structure all tests consistently
4. **Mock only what you need** - Don't over-mock
5. **Test edge cases** - Null values, empty collections, invalid IDs
6. **Use FluentAssertions** - For readable assertions

## Running Tests

```bash
# Run all repository tests
dotnet test tests/WebShop.Infrastructure.Tests/

# Run specific test class
dotnet test --filter "FullyQualifiedName~CustomerRepositoryTests"

# Run specific test
dotnet test --filter "FullyQualifiedName~CustomerRepositoryTests.GetByIdAsync_ValidId_ReturnsCustomer"
```

## Troubleshooting

### "ExecuteSql not found" Error

If you see this error, you're trying to use the old SQLite-based approach. Remove any `SeedTestData()` methods and use mock data instead.

### "SetupQueryFirstOrDefault cannot be used with type arguments" Error

Remove the generic type argument:

```csharp
// ❌ Incorrect
_testDatabase.SetupQueryFirstOrDefault<Dictionary<string, object>>(mockData);

// ✅ Correct
_testDatabase.SetupQueryFirstOrDefault(mockData);
```

### Mapping Issues

If properties aren't mapping correctly, ensure:
1. Column names are lowercase
2. All required properties are in the mock dictionary
3. Data types match the entity properties

## Summary

- Use **mocked connections** for fast, isolated unit tests
- Create mock data using **`Dictionary<string, object>`** with lowercase keys
- Use **`DapperTestDatabase` helper methods** to configure mock return values
- Focus tests on **repository logic**, not SQL correctness
- Save **integration tests** for higher levels (service/API)
