# Unit Testing Guide

This guide covers unit testing standards, best practices, and implementation patterns for the WebShop solution, following Microsoft guidelines and industry best practices.

[← Back to README](../README.md)

## Table of Contents

- [Overview](#overview)
- [Test Project Structure](#test-project-structure)
- [Testing Standards](#testing-standards)
- [Test Organization](#test-organization)
- [Writing Tests](#writing-tests)
- [Running Tests](#running-tests)
- [Test Coverage](#test-coverage)
- [Best Practices](#best-practices)
- [Common Patterns](#common-patterns)
- [Troubleshooting](#troubleshooting)

---

## Overview

The solution includes comprehensive unit test projects organized by architectural layer. All tests follow Microsoft's recommended practices using xUnit, Moq, and FluentAssertions.

### Test Projects

- **`tests/WebShop.Api.Tests`** - Tests for API controllers, filters, and middleware
- **`tests/WebShop.Business.Tests`** - Tests for business services and validators
- **`tests/WebShop.Infrastructure.Tests`** - Tests for repositories and infrastructure services

### Why Unit Testing?

- **Early Bug Detection**: Catch issues before they reach production
- **Documentation**: Tests serve as executable documentation
- **Refactoring Confidence**: Safe refactoring with test coverage
- **Design Validation**: Tests validate architectural decisions
- **Regression Prevention**: Prevent bugs from reoccurring

---

## Test Project Structure

```
tests/
├── WebShop.Api.Tests/
│   ├── Controllers/          # Controller tests
│   ├── Filters/             # Filter tests
│   └── Middleware/          # Middleware tests
├── WebShop.Business.Tests/
│   ├── Services/            # Service layer tests
│   ├── Validators/          # FluentValidation tests
│   └── TestUtilities/       # Test helpers and builders
└── WebShop.Infrastructure.Tests/
    ├── Repositories/        # Repository tests
    └── Services/            # Infrastructure service tests
```

---

## Testing Standards

### Framework & Tools

| Tool | Purpose | Version |
|------|---------|---------|
| **xUnit** | Testing framework (Microsoft recommended) | 2.9.2 |
| **Moq** | Mocking framework for dependencies | 4.20.72 |
| **FluentAssertions** | Fluent assertion library for readable tests | 7.0.0 |
| **coverlet.collector** | Code coverage collection | 6.0.2 |

### Naming Conventions

**Test Files:**

- Format: `<ClassName>Tests.cs`
- Example: `CustomerServiceTests.cs`, `CustomerControllerTests.cs`

**Test Methods:**

- Format: `MethodName_State_ExpectedBehavior`
- Examples:
  - `GetByIdAsync_ValidId_ReturnsCustomerDto`
  - `CreateAsync_NullDto_ThrowsArgumentNullException`
  - `DeleteAsync_InvalidId_ReturnsFalse`

**Test Classes:**

- Format: `<ClassName>Tests`
- Must be `public`
- No constructor parameters (use fields for setup)

---

## Test Organization

### By Layer

Tests mirror the source code structure:

- **API Layer** → `WebShop.Api.Tests`
- **Business Layer** → `WebShop.Business.Tests`
- **Infrastructure Layer** → `WebShop.Infrastructure.Tests`

### By Type

Within each test project, organize by component type:

- `Controllers/` - API controller tests
- `Services/` - Business service tests
- `Repositories/` - Repository tests
- `Filters/` - Filter tests
- `Middleware/` - Middleware tests

### By Feature

Use `#region` directives to group related tests:

```csharp
#region GetByIdAsync Tests

[Fact]
public async Task GetByIdAsync_ValidId_ReturnsCustomerDto() { }

[Fact]
public async Task GetByIdAsync_InvalidId_ReturnsNull() { }

#endregion
```

---

## Writing Tests

### Arrange-Act-Assert (AAA) Pattern

All tests follow the **AAA** pattern for clarity and consistency:

```csharp
[Fact]
public async Task GetByIdAsync_ValidId_ReturnsCustomerDto()
{
    // Arrange
    const int customerId = 1;
    Customer customer = new Customer
    {
        Id = customerId,
        FirstName = "John",
        LastName = "Doe",
        Email = "john.doe@example.com"
    };

    _mockRepository
        .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
        .ReturnsAsync(customer);

    // Act
    CustomerDto? result = await _service.GetByIdAsync(customerId);

    // Assert
    result.Should().NotBeNull();
    result!.Id.Should().Be(customerId);
    result.FirstName.Should().Be("John");
    result.Email.Should().Be("john.doe@example.com");
    _mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
}
```

### Test Attributes

- **`[Fact]`** - Single-scenario test
- **`[Theory]`** - Parameterized test with `[InlineData]` or `[MemberData]`

```csharp
[Theory]
[InlineData(1, true)]
[InlineData(999, false)]
public async Task ExistsAsync_ReturnsExpectedResult(int id, bool expected)
{
    // Test implementation
}
```

### Mocking Dependencies

**Service Layer Example:**

```csharp
public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _mockRepository;
    private readonly Mock<ILogger<CustomerService>> _mockLogger;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _mockRepository = new Mock<ICustomerRepository>();
        _mockLogger = new Mock<ILogger<CustomerService>>();
        _service = new CustomerService(_mockRepository.Object, _mockLogger.Object);
    }
}
```

**Controller Layer Example:**

```csharp
public class CustomerControllerTests
{
    private readonly Mock<ICustomerService> _mockService;
    private readonly Mock<ILogger<CustomerController>> _mockLogger;
    private readonly CustomerController _controller;

    public CustomerControllerTests()
    {
        _mockService = new Mock<ICustomerService>();
        _mockLogger = new Mock<ILogger<CustomerController>>();
        _controller = new CustomerController(_mockService.Object, _mockLogger.Object);
    }
}
```

### Repository Tests with Mocked Connections

```csharp
public class CustomerRepositoryTests : IDisposable
{
    private readonly DapperTestDatabase _testDatabase;
    private readonly CustomerRepository _repository;

    public CustomerRepositoryTests()
    {
        _testDatabase = new DapperTestDatabase();
        _repository = new CustomerRepository(
            _testDatabase.ConnectionFactory,
            _testDatabase.TransactionManager,
            _testDatabase.LoggerFactory);
    }

    public void Dispose()
    {
        _testDatabase?.Dispose();
    }
}
```

---

## Running Tests

### Run All Tests

```bash
# From solution root
dotnet test

# With detailed output
dotnet test --verbosity normal

# With logger
dotnet test --logger "console;verbosity=detailed"
```

### Run Specific Test Project

```bash
dotnet test tests/WebShop.Business.Tests
dotnet test tests/WebShop.Api.Tests
dotnet test tests/WebShop.Infrastructure.Tests
```

### Run with Code Coverage

```bash
# Collect coverage
dotnet test --collect:"XPlat Code Coverage"

# Coverage reports are generated in: TestResults/{guid}/coverage.cobertura.xml
```

### Run Specific Test

```bash
# By fully qualified name
dotnet test --filter "FullyQualifiedName~CustomerServiceTests.GetByIdAsync_ValidId_ReturnsCustomerDto"

# By test class
dotnet test --filter "FullyQualifiedName~CustomerServiceTests"

# By test name pattern
dotnet test --filter "Name~GetByIdAsync"
```

### Run Tests in Parallel

```bash
# Tests run in parallel by default
dotnet test --parallel

# Disable parallel execution
dotnet test --no-parallel
```

---

## Test Coverage

### Coverage Goals

| Layer | Target Coverage | Focus Areas |
|-------|----------------|-------------|
| **Controllers** | 80%+ | HTTP status codes, request/response handling, error cases |
| **Services** | 85%+ | Business logic, validation, error handling, edge cases |
| **Repositories** | 90%+ | CRUD operations, soft delete, query filters, edge cases |

### What to Test

#### Controllers

- ✅ HTTP status codes (200, 201, 204, 404, 400)
- ✅ Request/response mapping
- ✅ Error handling and logging
- ✅ Model validation (handled by filters, but verify behavior)

#### Services

- ✅ Business logic and rules
- ✅ Input validation
- ✅ Error handling
- ✅ DTO mapping
- ✅ Edge cases and boundary conditions

#### Repositories

- ✅ CRUD operations
- ✅ Soft delete behavior
- ✅ Query filters (IsActive)
- ✅ FindAsync with predicates
- ✅ ExistsAsync with/without soft-deleted

### What NOT to Test

- ❌ Framework code (ASP.NET Core, Dapper, Npgsql)
- ❌ Third-party library code
- ❌ Simple property getters/setters
- ❌ Private methods (test through public API)

---

## Best Practices

### 1. Test Isolation

- Each test is **independent** and can run in any order
- Tests **don't share state**
- Use fresh mocked connections for each test

### 2. Mocking Strategy

- **Mock all external dependencies**: Database connections, HTTP clients, file system
- **Don't mock what you're testing**: Test the actual implementation
- **Setup query results**: Use `DapperTestDatabase` to simulate Dapper query results
- **Verify interactions**: Use `Verify()` to ensure methods are called correctly

```csharp
// ✅ Good: Setup Dapper query result
_testDatabase.SetupQueryFirstOrDefault(new Dictionary<string, object>
{
    { "id", 1 }, 
    { "firstname", "John" },
    { "lastname", "Doe" },
    { "email", "john@example.com" }
});

// ✅ Good: Verify repository was called
_mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
```

### 3. Deterministic Tests

- **Use fixed values**: Avoid `Random`, `DateTime.Now`, `Guid.NewGuid()` in test data
- **Use constants**: Define test data as constants or use builders

```csharp
// ✅ Good: Fixed values
const int customerId = 1;
const string email = "john.doe@example.com";

// ❌ Bad: Random values
var customerId = Random.Shared.Next(1, 1000);
```

### 4. Test Data Builders

Use test utilities to create consistent test data:

```csharp
// From TestDataBuilder
Customer customer = TestDataBuilder.CreateCustomer(id: 1, firstName: "John");
CreateCustomerDto dto = TestDataBuilder.CreateCreateCustomerDto();
```

### 5. Assertions

Use **FluentAssertions** for readable assertions:

```csharp
// ✅ Good: FluentAssertions
result.Should().NotBeNull();
result.Id.Should().Be(customerId);
result.Email.Should().Be("john.doe@example.com");
customers.Should().HaveCount(2);
customers.Should().Contain(c => c.Id == 1);

// ❌ Avoid: Standard assertions
Assert.NotNull(result);
Assert.Equal(customerId, result.Id);
```

### 6. Exception Testing

Test exception scenarios:

```csharp
[Fact]
public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
{
    // Arrange
    CreateCustomerDto? createDto = null;

    // Act
    Func<Task> act = async () => await _service.CreateAsync(createDto!);

    // Assert
    await act.Should().ThrowAsync<ArgumentNullException>();
    _mockRepository.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
}
```

### 7. Async Testing

Always use `async Task` for async methods:

```csharp
// ✅ Good
[Fact]
public async Task GetByIdAsync_ValidId_ReturnsCustomerDto()
{
    CustomerDto? result = await _service.GetByIdAsync(1);
    // ...
}

// ❌ Bad: Blocking async
[Fact]
public void GetByIdAsync_ValidId_ReturnsCustomerDto()
{
    CustomerDto? result = _service.GetByIdAsync(1).Result; // Deadlock risk
}
```

---

## Common Patterns

### Service Layer Testing

```csharp
public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _mockRepository;
    private readonly Mock<ILogger<CustomerService>> _mockLogger;
    private readonly CustomerService _service;

    public CustomerServiceTests()
    {
        _mockRepository = new Mock<ICustomerRepository>();
        _mockLogger = new Mock<ILogger<CustomerService>>();
        _service = new CustomerService(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsCustomerDto()
    {
        // Arrange
        const int customerId = 1;
        Customer customer = new Customer { Id = customerId, FirstName = "John", Email = "john@example.com" };
        _mockRepository.Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        CustomerDto? result = await _service.GetByIdAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(customerId);
        _mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Controller Layer Testing

```csharp
public class CustomerControllerTests
{
    private readonly Mock<ICustomerService> _mockService;
    private readonly Mock<ILogger<CustomerController>> _mockLogger;
    private readonly CustomerController _controller;

    public CustomerControllerTests()
    {
        _mockService = new Mock<ICustomerService>();
        _mockLogger = new Mock<ILogger<CustomerController>>();
        _controller = new CustomerController(_mockService.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task GetById_ValidId_ReturnsOkWithCustomer()
    {
        // Arrange
        const int customerId = 1;
        CustomerDto customer = new CustomerDto { Id = customerId, FirstName = "John" };
        _mockService.Setup(s => s.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        // Act
        ActionResult<Response<CustomerDto>> result = await _controller.GetById(customerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<CustomerDto>? response = okResult!.Value as Response<CustomerDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(customerId);
        response.Succeeded.Should().BeTrue();
    }
}
```

### Repository Layer Testing

```csharp
public class CustomerRepositoryTests : IDisposable
{
    private readonly DapperTestDatabase _testDatabase;
    private readonly CustomerRepository _repository;

    public CustomerRepositoryTests()
    {
        _testDatabase = new DapperTestDatabase();
        _repository = new CustomerRepository(
            _testDatabase.ConnectionFactory,
            _testDatabase.TransactionManager,
            _testDatabase.LoggerFactory);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsCustomer()
    {
        // Arrange
        const int customerId = 1;
        IDictionary<string, object> dynamicCustomer = new Dictionary<string, object>
        {
            { "id", 1 },
            { "firstname", "John" },
            { "lastname", "Doe" },
            { "email", "john@example.com" },
            { "isactive", 1 }
        };
        _testDatabase.SetupQueryFirstOrDefault(dynamicCustomer);

        // Act
        Customer? result = await _repository.GetByIdAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(customerId);
        result.FirstName.Should().Be("John");
    }

    public void Dispose()
    {
        _testDatabase?.Dispose();
    }
}
```

---

## Troubleshooting

### Common Issues

**Tests fail intermittently:**

- Ensure tests are isolated (no shared state)
- Fresh mocked connections for each test
- Check for race conditions in parallel execution

**Mock not working:**

- Verify mock is set up before the Act phase
- Check that the method signature matches exactly
- Use `It.IsAny<T>()` for flexible parameter matching
- For Dapper tests, ensure `SetupQuery*` methods are called with correct data structure

**Dapper mocking issues:**

- Ensure dynamic objects match database column names (lowercase)
- Setup appropriate return values for each query type (single, multiple, scalar)
- Verify connection factory and transaction manager mocks are properly configured

**Coverage not collected:**

- Ensure `coverlet.collector` package is referenced
- Use `--collect:"XPlat Code Coverage"` flag
- Check `TestResults/` directory for coverage files

**Tests timeout:**

- Check for deadlocks (avoid `.Result` or `.Wait()`)
- Use `ConfigureAwait(false)` in production code
- Verify cancellation tokens are used correctly

---

## Example Test Files

Reference implementations:

- **Service Tests**: `tests/WebShop.Business.Tests/Services/CustomerServiceTests.cs`
- **Controller Tests**: `tests/WebShop.Api.Tests/Controllers/CustomerControllerTests.cs`
- **Repository Tests**: `tests/WebShop.Infrastructure.Tests/Repositories/CustomerRepositoryTests.cs` (Dapper with mocked connections)
- **Test Utilities**: `tests/WebShop.Business.Tests/TestUtilities/TestDataBuilder.cs`
- **Dapper Test Helper**: `tests/WebShop.Infrastructure.Tests/Helpers/DapperTestDatabase.cs`

---

## Adding New Tests

### Step-by-Step Guide

1. **Identify what to test**: Service method, controller action, repository operation

2. **Create test file**: `<ClassName>Tests.cs` in appropriate folder

3. **Set up test class**:

   ```csharp
   public class MyServiceTests
   {
       private readonly Mock<IDependency> _mockDependency;
       private readonly MyService _service;

       public MyServiceTests()
       {
           _mockDependency = new Mock<IDependency>();
           _service = new MyService(_mockDependency.Object);
       }
   }
   ```

4. **Write test methods**:
   - Follow AAA pattern
   - Use descriptive names: `MethodName_State_ExpectedBehavior`
   - Test success and failure scenarios

5. **Add XML comments**:

   ```csharp
   /// <summary>
   /// Verifies that GetByIdAsync returns the correct customer when a valid ID is provided.
   /// </summary>
   [Fact]
   public async Task GetByIdAsync_ValidId_ReturnsCustomerDto() { }
   ```

6. **Run tests**: `dotnet test --filter "FullyQualifiedName~MyServiceTests"`

---

## Resources

### Primary Documentation

- [Testing Comprehensive Guide](./testing-comprehensive-guide.md) - Strategic testing standards and coverage requirements

### Testing Frameworks & Tools

- [xUnit Documentation](https://xunit.net/)
- [Moq Quickstart](https://github.com/moq/moq4/wiki/Quickstart)
- [FluentAssertions Documentation](https://fluentassertions.com/)
- [Microsoft Testing Best Practices](https://learn.microsoft.com/en-us/dotnet/core/testing/)

---

**Remember**: Good tests are fast, isolated, repeatable, self-validating, and timely (FIRST principles).
