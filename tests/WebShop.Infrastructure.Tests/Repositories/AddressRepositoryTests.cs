using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.Infrastructure.Tests.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Repositories;

/// <summary>
/// Unit tests for AddressRepository using mocked database connections.
/// </summary>
[Trait("Category", "Unit")]
public class AddressRepositoryTests : IDisposable
{
    private readonly DapperTestDatabase _testDatabase;
    private readonly AddressRepository _repository;
    private readonly Mock<ILoggerFactory> _mockLoggerFactory;

    public AddressRepositoryTests()
    {
        _testDatabase = new DapperTestDatabase();
        _mockLoggerFactory = new Mock<ILoggerFactory>();
        _repository = new AddressRepository(
            _testDatabase.ConnectionFactory,
            _testDatabase.TransactionManager,
            _mockLoggerFactory.Object);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsAddress()
    {
        // Arrange
        Dictionary<string, object> mockAddress = new Dictionary<string, object>
        {
            { "id", 1 },
            { "customerid", 1 },
            { "firstname", "John" },
            { "lastname", "Doe" },
            { "address1", "123 Main St" },
            { "city", "New York" },
            { "zip", "10001" },
            { "isactive", true },
            { "created", DateTime.UtcNow },
            { "createdby", 1 },
            { "updatedby", 1 }
        };
        _testDatabase.SetupQueryFirstOrDefault(mockAddress);

        // Act
        Address? result = await _repository.GetByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        _testDatabase.SetupQueryFirstOrDefault(null);

        // Act
        Address? result = await _repository.GetByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveAddresses()
    {
        // Arrange
        Dictionary<string, object>[] mockAddresses = new[]
        {
            new Dictionary<string, object>
            {
                { "id", 1 }, { "customerid", 1 }, { "firstname", "John" }, { "lastname", "Doe" },
                { "address1", "123 Main St" }, { "city", "New York" }, { "zip", "10001" },
                { "isactive", true }, { "created", DateTime.UtcNow }, { "createdby", 1 }, { "updatedby", 1 }
            },
            new Dictionary<string, object>
            {
                { "id", 2 }, { "customerid", 1 }, { "firstname", "John" }, { "lastname", "Doe" },
                { "address1", "456 Oak Ave" }, { "city", "Boston" }, { "zip", "02101" },
                { "isactive", true }, { "created", DateTime.UtcNow }, { "createdby", 1 }, { "updatedby", 1 }
            },
            new Dictionary<string, object>
            {
                { "id", 3 }, { "customerid", 2 }, { "firstname", "Jane" }, { "lastname", "Smith" },
                { "address1", "789 Pine Rd" }, { "city", "Chicago" }, { "zip", "60601" },
                { "isactive", true }, { "created", DateTime.UtcNow }, { "createdby", 1 }, { "updatedby", 1 }
            }
        };
        _testDatabase.SetupQuery(mockAddresses);

        // Act
        IReadOnlyList<Address> result = await _repository.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ValidCustomerId_ReturnsAddresses()
    {
        // Arrange
        Dictionary<string, object>[] mockAddresses = new[]
        {
            new Dictionary<string, object>
            {
                { "id", 1 }, { "customerid", 1 }, { "firstname", "John" }, { "lastname", "Doe" },
                { "address1", "123 Main St" }, { "city", "New York" }, { "zip", "10001" },
                { "isactive", true }, { "created", DateTime.UtcNow }, { "createdby", 1 }, { "updatedby", 1 }
            },
            new Dictionary<string, object>
            {
                { "id", 2 }, { "customerid", 1 }, { "firstname", "John" }, { "lastname", "Doe" },
                { "address1", "456 Oak Ave" }, { "city", "Boston" }, { "zip", "02101" },
                { "isactive", true }, { "created", DateTime.UtcNow }, { "createdby", 1 }, { "updatedby", 1 }
            }
        };
        _testDatabase.SetupQuery(mockAddresses);

        // Act
        List<Address> result = await _repository.GetByCustomerIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(a => a.CustomerId == 1).Should().BeTrue();
    }

    public void Dispose()
    {
        _testDatabase?.Dispose();
    }
}
