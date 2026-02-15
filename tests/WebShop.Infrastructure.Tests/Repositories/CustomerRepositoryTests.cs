using FluentAssertions;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.Infrastructure.Tests.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Repositories;

/// <summary>
/// Repository tests for CustomerRepository using real PostgreSQL (integration DB).
/// </summary>
[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class CustomerRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly CustomerRepository _repository;

    public CustomerRepositoryTests(Helpers.TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        _repository = new CustomerRepository(
            _fixture.ConnectionFactory,
            null,
            loggerFactory);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsCustomer()
    {
        await _fixture.ResetDatabaseAsync();

        Customer customer = new()
        {
            FirstName = "John",
            LastName = "Doe",
            Gender = "male",
            Email = "john.doe@example.com",
            CreatedBy = 1,
            UpdatedBy = 1
        };
        await _repository.AddAsync(customer);

        Customer? result = await _repository.GetByIdAsync(customer.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(customer.Id);
        result.FirstName.Should().Be("John");
        result.Email.Should().Be("john.doe@example.com");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        Customer? result = await _repository.GetByIdAsync(999999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveCustomers()
    {
        await _fixture.ResetDatabaseAsync();

        await _repository.AddAsync(new Customer { FirstName = "John", LastName = "Doe", Gender = "male", Email = "john@example.com", CreatedBy = 1, UpdatedBy = 1 });
        await _repository.AddAsync(new Customer { FirstName = "Jane", LastName = "Smith", Gender = "female", Email = "jane@example.com", CreatedBy = 1, UpdatedBy = 1 });

        IReadOnlyList<Customer> result = await _repository.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByEmailAsync_ValidEmail_ReturnsCustomer()
    {
        await _fixture.ResetDatabaseAsync();

        await _repository.AddAsync(new Customer { FirstName = "John", LastName = "Doe", Gender = "male", Email = "john.doe@example.com", CreatedBy = 1, UpdatedBy = 1 });

        Customer? result = await _repository.GetByEmailAsync("john.doe@example.com");

        result.Should().NotBeNull();
        result!.Email.Should().Be("john.doe@example.com");
        result.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetByEmailAsync_InvalidEmail_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        Customer? result = await _repository.GetByEmailAsync("nonexistent@example.com");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ExistingId_ReturnsTrue()
    {
        await _fixture.ResetDatabaseAsync();

        Customer customer = new() { FirstName = "John", LastName = "Doe", Gender = "male", Email = "exists@example.com", CreatedBy = 1, UpdatedBy = 1 };
        await _repository.AddAsync(customer);

        bool result = await _repository.ExistsAsync(customer.Id);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_NonExistingId_ReturnsFalse()
    {
        await _fixture.ResetDatabaseAsync();

        bool result = await _repository.ExistsAsync(999999);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        await _fixture.ResetDatabaseAsync();

        IReadOnlyList<Customer> result = await _repository.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }
}
