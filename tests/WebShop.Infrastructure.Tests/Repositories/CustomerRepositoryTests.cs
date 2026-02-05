using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.Infrastructure.Tests.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Repositories;

/// <summary>
/// Unit tests for CustomerRepository using mocked database connections.
/// </summary>
[Trait("Category", "Unit")]
public class CustomerRepositoryTests : IDisposable
{
    private readonly DapperTestDatabase _testDatabase;
    private readonly CustomerRepository _repository;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;

    public CustomerRepositoryTests()
    {
        _testDatabase = new DapperTestDatabase();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _mockLoggerFactory.Setup(f => f.CreateLogger(It.IsAny<string>())).Returns(Mock.Of<ILogger>());
        _repository = new CustomerRepository(
            _testDatabase.ConnectionFactory,
            _testDatabase.TransactionManager,
            _mockLoggerFactory.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsCustomer()
    {
        // Arrange
        const int customerId = 1;
        Customer testCustomer = new Customer
        {
            Id = customerId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = 1,
            UpdatedBy = 1
        };

        // Create dynamic object that Dapper will map (using lowercase column names as per database)
        Dictionary<string, object> dynamicCustomer = new Dictionary<string, object>
        {
            { "id", customerId },
            { "firstname", "John" },
            { "lastname", "Doe" },
            { "email", "john.doe@example.com" },
            { "isactive", true },
            { "created", DateTime.UtcNow },
            { "createdby", 1 },
            { "updatedby", 1 }
        };

        _testDatabase.SetupQueryFirstOrDefault(dynamicCustomer);

        // Act
        Customer? result = await _repository.GetByIdAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(customerId);
        result.FirstName.Should().Be("John");
        result.Email.Should().Be("john.doe@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int customerId = 999;
        _testDatabase.SetupQueryFirstOrDefault(null);

        // Act
        Customer? result = await _repository.GetByIdAsync(customerId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveCustomers()
    {
        // Arrange
        Dictionary<string, object>[] customers = new[]
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
        result.All(c => c.IsActive).Should().BeTrue();
    }

    #endregion

    #region GetByEmailAsync Tests

    [Fact]
    public async Task GetByEmailAsync_ValidEmail_ReturnsCustomer()
    {
        // Arrange
        const string email = "john.doe@example.com";
        Dictionary<string, object> dynamicCustomer = new Dictionary<string, object>
        {
            { "id", 1 },
            { "firstname", "John" },
            { "lastname", "Doe" },
            { "email", email },
            { "isactive", true },
            { "created", DateTime.UtcNow },
            { "createdby", 1 },
            { "updatedby", 1 }
        };

        _testDatabase.SetupQueryFirstOrDefault(dynamicCustomer);

        // Act
        Customer? result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
        result.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetByEmailAsync_InvalidEmail_ReturnsNull()
    {
        // Arrange
        const string email = "nonexistent@example.com";
        _testDatabase.SetupQueryFirstOrDefault(null);

        // Act
        Customer? result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region ExistsAsync Tests

    [Fact]
    public async Task ExistsAsync_ExistingId_ReturnsTrue()
    {
        // Arrange
        const int customerId = 1;
        _testDatabase.SetupScalar(true);

        // Act
        bool result = await _repository.ExistsAsync(customerId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistingId_ReturnsFalse()
    {
        // Arrange
        const int customerId = 999;
        _testDatabase.SetupScalar(false);

        // Act
        bool result = await _repository.ExistsAsync(customerId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Edge Cases Tests

    [Fact]
    public async Task GetByIdAsync_WithCancellation_ThrowsOperationCanceledException()
    {
        // Arrange
        using CancellationTokenSource cts = new();
        cts.Cancel();

        // Act & Assert
        Func<Task> act = async () => await _repository.GetByIdAsync(1, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
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

    #endregion

    public void Dispose()
    {
        _testDatabase?.Dispose();
    }
}
