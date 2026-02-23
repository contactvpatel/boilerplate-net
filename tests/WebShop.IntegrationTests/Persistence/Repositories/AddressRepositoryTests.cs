using FluentAssertions;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.IntegrationTests.Fixtures;
using Xunit;

namespace WebShop.IntegrationTests.Persistence.Repositories;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class AddressRepositoryTests
{
    private readonly TestDatabaseFixture _fixture;
    private readonly AddressRepository _addressRepository;
    private readonly CustomerRepository _customerRepository;

    public AddressRepositoryTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        _addressRepository = new AddressRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _customerRepository = new CustomerRepository(_fixture.ConnectionFactory, null, loggerFactory);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsAddress()
    {
        await _fixture.ResetDatabaseAsync();

        Customer customer = new() { FirstName = "John", LastName = "Doe", Gender = "male", Email = "john@example.com", CreatedBy = 1, UpdatedBy = 1 };
        await _customerRepository.AddAsync(customer);

        Address address = new() { CustomerId = customer.Id, FirstName = "John", LastName = "Doe", Address1 = "123 Main St", City = "New York", Zip = "10001", CreatedBy = 1, UpdatedBy = 1 };
        await _addressRepository.AddAsync(address);

        Address? result = await _addressRepository.GetByIdAsync(address.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(address.Id);
        result.FirstName.Should().Be("John");
        result.Address1.Should().Be("123 Main St");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        Address? result = await _addressRepository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsAllActiveAddresses()
    {
        await _fixture.ResetDatabaseAsync();

        Customer customer = new() { FirstName = "John", LastName = "Doe", Gender = "male", Email = "john@example.com", CreatedBy = 1, UpdatedBy = 1 };
        await _customerRepository.AddAsync(customer);

        await _addressRepository.AddAsync(new Address { CustomerId = customer.Id, FirstName = "John", LastName = "Doe", Address1 = "123 Main St", City = "New York", Zip = "10001", CreatedBy = 1, UpdatedBy = 1 });
        await _addressRepository.AddAsync(new Address { CustomerId = customer.Id, FirstName = "John", LastName = "Doe", Address1 = "456 Oak Ave", City = "Boston", Zip = "02101", CreatedBy = 1, UpdatedBy = 1 });

        IReadOnlyList<Address> result = await _addressRepository.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ValidCustomerId_ReturnsAddresses()
    {
        await _fixture.ResetDatabaseAsync();

        Customer customer = new() { FirstName = "John", LastName = "Doe", Gender = "male", Email = "john@example.com", CreatedBy = 1, UpdatedBy = 1 };
        await _customerRepository.AddAsync(customer);

        await _addressRepository.AddAsync(new Address { CustomerId = customer.Id, FirstName = "John", LastName = "Doe", Address1 = "123 Main St", City = "New York", Zip = "10001", CreatedBy = 1, UpdatedBy = 1 });
        await _addressRepository.AddAsync(new Address { CustomerId = customer.Id, FirstName = "John", LastName = "Doe", Address1 = "456 Oak Ave", City = "Boston", Zip = "02101", CreatedBy = 1, UpdatedBy = 1 });

        List<Address> result = await _addressRepository.GetByCustomerIdAsync(customer.Id);

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.All(a => a.CustomerId == customer.Id).Should().BeTrue();
    }
}
