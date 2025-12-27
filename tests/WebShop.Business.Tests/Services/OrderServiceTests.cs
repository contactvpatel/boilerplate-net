using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Business.DTOs;
using WebShop.Business.Services;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using Xunit;

namespace WebShop.Business.Tests.Services;

/// <summary>
/// Unit tests for OrderService.
/// </summary>
[Trait("Category", "Unit")]
public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _mockOrderRepository;
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly Mock<IAddressRepository> _mockAddressRepository;
    private readonly Mock<ILogger<OrderService>> _mockLogger;
    private readonly OrderService _service;

    public OrderServiceTests()
    {
        _mockOrderRepository = new Mock<IOrderRepository>();
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _mockAddressRepository = new Mock<IAddressRepository>();
        _mockLogger = new Mock<ILogger<OrderService>>();
        _service = new OrderService(_mockOrderRepository.Object, _mockCustomerRepository.Object, _mockAddressRepository.Object, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsOrderDto()
    {
        // Arrange
        const int orderId = 1;
        Order order = new Order
        {
            Id = orderId,
            CustomerId = 1,
            ShippingAddressId = 1,
            OrderTimestamp = DateTime.UtcNow
        };

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        OrderDto? result = await _service.GetByIdAsync(orderId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int orderId = 999;
        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        OrderDto? result = await _service.GetByIdAsync(orderId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllOrders()
    {
        // Arrange
        List<Order> orders = new List<Order>
        {
            new() { Id = 1, CustomerId = 1, ShippingAddressId = 1 },
            new() { Id = 2, CustomerId = 2, ShippingAddressId = 2 }
        };

        _mockOrderRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        IReadOnlyList<OrderDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesOrder()
    {
        // Arrange
        CreateOrderDto createDto = new CreateOrderDto
        {
            CustomerId = 1,
            ShippingAddressId = 1,
            ShippingCost = 10.00m
        };

        Customer customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        Address address = new Address { Id = 1, CustomerId = 1, Address1 = "123 Main St" };
        Order order = new Order
        {
            Id = 1,
            CustomerId = 1,
            ShippingAddressId = 1,
            Total = 100.00m
        };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken ct) => o);

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        OrderDto result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(1);
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidCustomerId_ThrowsArgumentException()
    {
        // Arrange
        CreateOrderDto createDto = new CreateOrderDto
        {
            CustomerId = 999,
            ShippingAddressId = 1
        };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(createDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidAddressId_ThrowsArgumentException()
    {
        // Arrange
        CreateOrderDto createDto = new CreateOrderDto
        {
            CustomerId = 1,
            ShippingAddressId = 999
        };

        Customer customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address?)null);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(createDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_AddressNotBelongsToCustomer_ThrowsArgumentException()
    {
        // Arrange
        CreateOrderDto createDto = new CreateOrderDto
        {
            CustomerId = 1,
            ShippingAddressId = 1
        };

        Customer customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        Address address = new Address { Id = 1, CustomerId = 2, Address1 = "123 Main St" }; // Different customer

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(createDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateOrderDto? createDto = null;

        // Act
        Func<Task> act = async () => await _service.CreateAsync(createDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetByCustomerIdAsync Tests

    [Fact]
    public async Task GetByCustomerIdAsync_ValidCustomerId_ReturnsOrders()
    {
        // Arrange
        const int customerId = 1;
        List<Order> orders = new List<Order>
        {
            new() { Id = 1, CustomerId = customerId, ShippingAddressId = 1 }
        };

        _mockOrderRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        IReadOnlyList<OrderDto> result = await _service.GetByCustomerIdAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    #endregion

    #region GetByDateRangeAsync Tests

    [Fact]
    public async Task GetByDateRangeAsync_ValidDateRange_ReturnsOrders()
    {
        // Arrange
        DateTime startDate = DateTime.UtcNow.AddDays(-7);
        DateTime endDate = DateTime.UtcNow;
        List<Order> orders = new List<Order>
        {
            new() { Id = 1, CustomerId = 1, OrderTimestamp = DateTime.UtcNow.AddDays(-3) }
        };

        _mockOrderRepository
            .Setup(r => r.GetByDateRangeAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        IReadOnlyList<OrderDto> result = await _service.GetByDateRangeAsync(startDate, endDate);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidId_UpdatesOrder()
    {
        // Arrange
        const int orderId = 1;
        UpdateOrderDto updateDto = new UpdateOrderDto
        {
            Total = 200.00m,
            ShippingCost = 10.00m
        };

        Order existingOrder = new Order
        {
            Id = orderId,
            CustomerId = 1,
            Total = 100.00m
        };

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        OrderDto? result = await _service.UpdateAsync(orderId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Total.Should().Be(200.00m);
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int orderId = 999;
        UpdateOrderDto updateDto = new UpdateOrderDto { Total = 200.00m };

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        OrderDto? result = await _service.UpdateAsync(orderId, updateDto);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region PatchAsync Tests

    [Fact]
    public async Task PatchAsync_ValidId_WithChanges_PatchesOrder()
    {
        // Arrange
        const int orderId = 1;
        UpdateOrderDto patchDto = new UpdateOrderDto
        {
            Total = 150.00m
        };

        Order existingOrder = new Order
        {
            Id = orderId,
            CustomerId = 1,
            Total = 100.00m
        };

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        OrderDto? result = await _service.PatchAsync(orderId, patchDto);

        // Assert
        result.Should().NotBeNull();
        result!.Total.Should().Be(150.00m);
    }

    [Fact]
    public async Task PatchAsync_ValidId_NoChanges_ReturnsOrderWithoutSaving()
    {
        // Arrange
        const int orderId = 1;
        UpdateOrderDto patchDto = new UpdateOrderDto
        {
            Total = 100.00m // Same as existing
        };

        Order existingOrder = new Order
        {
            Id = orderId,
            CustomerId = 1,
            Total = 100.00m
        };

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        OrderDto? result = await _service.PatchAsync(orderId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesOrder()
    {
        // Arrange
        const int orderId = 1;
        Order order = new Order
        {
            Id = orderId,
            CustomerId = 1,
            ShippingAddressId = 1
        };

        _mockOrderRepository
            .Setup(r => r.ExistsAsync(orderId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        _mockOrderRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await _service.DeleteAsync(orderId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int orderId = 999;

        _mockOrderRepository
            .Setup(r => r.ExistsAsync(orderId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.DeleteAsync(orderId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Batch Operations Tests

    [Fact]
    public async Task UpdateBatchAsync_ValidUpdates_UpdatesOrders()
    {
        // Arrange
        List<(int Id, UpdateOrderDto UpdateDto)> updates = new List<(int, UpdateOrderDto)>
        {
            (1, new UpdateOrderDto { Total = 200.00m }),
            (2, new UpdateOrderDto { Total = 300.00m })
        };

        List<Order> orders = new List<Order>
        {
            new() { Id = 1, CustomerId = 1, Total = 100.00m },
            new() { Id = 2, CustomerId = 1, Total = 150.00m }
        };

        _mockOrderRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<OrderDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task UpdateBatchAsync_SomeOrdersNotFound_SkipsMissingOrders()
    {
        // Arrange
        List<(int Id, UpdateOrderDto UpdateDto)> updates = new List<(int, UpdateOrderDto)>
        {
            (1, new UpdateOrderDto { Total = 200.00m }),
            (2, new UpdateOrderDto { Total = 300.00m }),
            (999, new UpdateOrderDto { Total = 400.00m }) // Not found
        };

        List<Order> orders = new List<Order>
        {
            new() { Id = 1, CustomerId = 1, Total = 100.00m },
            new() { Id = 2, CustomerId = 1, Total = 150.00m }
            // Order 999 is missing
        };

        _mockOrderRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<OrderDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Only 2 orders updated, 1 skipped
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteBatchAsync_ValidIds_DeletesOrders()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2 };
        List<Order> orders = new List<Order>
        {
            new() { Id = 1, CustomerId = 1, ShippingAddressId = 1 },
            new() { Id = 2, CustomerId = 1, ShippingAddressId = 1 }
        };

        _mockOrderRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _mockOrderRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<int> result = await _service.DeleteBatchAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateAsync_CustomerNotFound_ThrowsArgumentException()
    {
        // Arrange
        CreateOrderDto createDto = new CreateOrderDto
        {
            CustomerId = 999,
            ShippingAddressId = 1
        };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateAsync(createDto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Customer with ID 999 not found*");
    }

    [Fact]
    public async Task CreateAsync_ShippingAddressNotFound_ThrowsArgumentException()
    {
        // Arrange
        CreateOrderDto createDto = new CreateOrderDto
        {
            CustomerId = 1,
            ShippingAddressId = 999
        };

        Customer customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address?)null);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateAsync(createDto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Shipping address with ID 999 not found*");
    }

    [Fact]
    public async Task CreateAsync_AddressDoesNotBelongToCustomer_ThrowsArgumentException()
    {
        // Arrange
        CreateOrderDto createDto = new CreateOrderDto
        {
            CustomerId = 1,
            ShippingAddressId = 1
        };

        Customer customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        Address address = new Address { Id = 1, CustomerId = 2, Address1 = "Address", City = "City" }; // Different customer

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateAsync(createDto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Shipping address 1 does not belong to customer 1*");
    }

    [Fact]
    public async Task CreateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        CreateOrderDto createDto = new CreateOrderDto
        {
            CustomerId = 1,
            ShippingAddressId = 1
        };

        Customer customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        Address address = new Address { Id = 1, CustomerId = 1, Address1 = "Address", City = "City" };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        _mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Order()));

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.CreateAsync(createDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int orderId = 1;
        UpdateOrderDto updateDto = new UpdateOrderDto { Total = 200.00m };
        Order existingOrder = new Order { Id = orderId, CustomerId = 1, Total = 100.00m };

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.UpdateAsync(orderId, updateDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PatchAsync_NoChanges_DoesNotCallUpdate()
    {
        // Arrange
        const int orderId = 1;
        Order existingOrder = new Order
        {
            Id = orderId,
            CustomerId = 1,
            Total = 100.00m,
            ShippingCost = 10.00m
        };

        UpdateOrderDto patchDto = new UpdateOrderDto
        {
            Total = 100.00m, // Same value
            ShippingCost = 10.00m // Same value
        };

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        OrderDto? result = await _service.PatchAsync(orderId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockOrderRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int orderId = 1;
        Order existingOrder = new Order { Id = orderId, CustomerId = 1, Total = 100.00m };

        _mockOrderRepository
            .Setup(r => r.ExistsAsync(orderId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        _mockOrderRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteAsync(orderId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<(int Id, UpdateOrderDto UpdateDto)> updates = new List<(int, UpdateOrderDto)>
        {
            (1, new UpdateOrderDto { Total = 200.00m })
        };

        List<Order> orders = new List<Order>
        {
            new() { Id = 1, CustomerId = 1, Total = 100.00m }
        };

        _mockOrderRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.UpdateBatchAsync(updates);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task DeleteBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<int> ids = new List<int> { 1 };
        List<Order> orders = new List<Order>
        {
            new() { Id = 1, CustomerId = 1, Total = 100.00m }
        };

        _mockOrderRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Order, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        _mockOrderRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteBatchAsync(ids);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

}
