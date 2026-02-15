using FluentAssertions;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.Infrastructure.Tests.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Repositories;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class OrderRepositoryTests
{
    private readonly Helpers.TestDatabaseFixture _fixture;
    private readonly OrderRepository _orderRepository;
    private readonly CustomerRepository _customerRepository;
    private readonly AddressRepository _addressRepository;

    public OrderRepositoryTests(Helpers.TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        _orderRepository = new OrderRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _customerRepository = new CustomerRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _addressRepository = new AddressRepository(_fixture.ConnectionFactory, null, loggerFactory);
    }

    private async Task<(Customer Customer, Address Address)> CreateCustomerWithAddressAsync()
    {
        Customer customer = new() { FirstName = "John", LastName = "Doe", Gender = "male", Email = "john@example.com", CreatedBy = 1, UpdatedBy = 1 };
        await _customerRepository.AddAsync(customer);
        Address address = new() { CustomerId = customer.Id, FirstName = "John", LastName = "Doe", Address1 = "123 Main St", City = "New York", Zip = "10001", CreatedBy = 1, UpdatedBy = 1 };
        await _addressRepository.AddAsync(address);
        return (customer, address);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsOrder()
    {
        await _fixture.ResetDatabaseAsync();

        var (customer, address) = await CreateCustomerWithAddressAsync();
        Order order = new() { CustomerId = customer.Id, OrderTimestamp = DateTime.UtcNow, ShippingAddressId = address.Id, Total = 99.99m, ShippingCost = 5.00m, CreatedBy = 1, UpdatedBy = 1 };
        await _orderRepository.AddAsync(order);

        Order? result = await _orderRepository.GetByIdAsync(order.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(order.Id);
        result.CustomerId.Should().Be(customer.Id);
        result.Total.Should().Be(99.99m);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        Order? result = await _orderRepository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveOrders()
    {
        await _fixture.ResetDatabaseAsync();

        var (customer1, address1) = await CreateCustomerWithAddressAsync();
        Customer customer2 = new() { FirstName = "Jane", LastName = "Smith", Gender = "female", Email = "jane@example.com", CreatedBy = 1, UpdatedBy = 1 };
        await _customerRepository.AddAsync(customer2);
        Address address2 = new() { CustomerId = customer2.Id, FirstName = "Jane", LastName = "Smith", Address1 = "456 Oak Ave", City = "Boston", Zip = "02101", CreatedBy = 1, UpdatedBy = 1 };
        await _addressRepository.AddAsync(address2);

        await _orderRepository.AddAsync(new Order { CustomerId = customer1.Id, OrderTimestamp = DateTime.UtcNow, ShippingAddressId = address1.Id, Total = 99.99m, ShippingCost = 5.00m, CreatedBy = 1, UpdatedBy = 1 });
        await _orderRepository.AddAsync(new Order { CustomerId = customer2.Id, OrderTimestamp = DateTime.UtcNow, ShippingAddressId = address2.Id, Total = 149.99m, ShippingCost = 0m, CreatedBy = 1, UpdatedBy = 1 });

        IReadOnlyList<Order> result = await _orderRepository.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_ValidParams_ReturnsPagedResult()
    {
        await _fixture.ResetDatabaseAsync();

        var (customer, address) = await CreateCustomerWithAddressAsync();
        for (int i = 0; i < 10; i++)
        {
            await _orderRepository.AddAsync(new Order { CustomerId = customer.Id, OrderTimestamp = DateTime.UtcNow, ShippingAddressId = address.Id, Total = 99.99m + i, ShippingCost = 5.00m, CreatedBy = 1, UpdatedBy = 1 });
        }

        var (items, totalCount) = await _orderRepository.GetPagedAsync(1, 10);

        items.Should().NotBeNull();
        items.Should().HaveCount(10);
        totalCount.Should().Be(10);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ValidCustomerId_ReturnsOrders()
    {
        await _fixture.ResetDatabaseAsync();

        var (customer, address) = await CreateCustomerWithAddressAsync();
        await _orderRepository.AddAsync(new Order { CustomerId = customer.Id, OrderTimestamp = DateTime.UtcNow, ShippingAddressId = address.Id, Total = 99.99m, ShippingCost = 5.00m, CreatedBy = 1, UpdatedBy = 1 });

        List<Order> result = await _orderRepository.GetByCustomerIdAsync(customer.Id);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].CustomerId.Should().Be(customer.Id);
    }

    [Fact]
    public async Task FindByIdsAsync_ValidIds_ReturnsOrders()
    {
        await _fixture.ResetDatabaseAsync();

        var (customer, address) = await CreateCustomerWithAddressAsync();
        Order order = new() { CustomerId = customer.Id, OrderTimestamp = DateTime.UtcNow, ShippingAddressId = address.Id, Total = 99.99m, ShippingCost = 5.00m, CreatedBy = 1, UpdatedBy = 1 };
        await _orderRepository.AddAsync(order);

        IReadOnlyList<Order> result = await _orderRepository.FindByIdsAsync(new[] { order.Id, 999 });

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindByIdsAsync_EmptyList_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();

        IReadOnlyList<Order> result = await _orderRepository.FindByIdsAsync(Array.Empty<int>());

        result.Should().BeEmpty();
    }
}
