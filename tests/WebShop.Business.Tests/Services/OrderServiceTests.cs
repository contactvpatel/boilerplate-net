using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Business.DTOs;
using WebShop.Business.Models;
using WebShop.Business.Services;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;
using Xunit;

namespace WebShop.Business.Tests.Services;

/// <summary>
/// Unit tests for OrderService.
/// </summary>
[Trait("Category", "Unit")]
public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> mockOrderRepository = new();
    private readonly Mock<ICustomerRepository> mockCustomerRepository = new();
    private readonly Mock<IAddressRepository> mockAddressRepository = new();
    private readonly Mock<IUnitOfWork> mockUnitOfWork = new();
    private readonly Mock<ILogger<OrderService>> mockLogger = new();
    private readonly OrderService service;

    public OrderServiceTests()
    {
        service = new OrderService(mockOrderRepository.Object, mockCustomerRepository.Object, mockAddressRepository.Object, mockUnitOfWork.Object, mockLogger.Object);
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

        mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        // Act
        Result<OrderDto> result = await service.GetByIdAsync(orderId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int orderId = 999;
        mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        Result<OrderDto> result = await service.GetByIdAsync(orderId);

        // Assert
        result.IsNotFound.Should().BeTrue();
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

        mockOrderRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        IReadOnlyList<OrderDto> result = await service.GetAllAsync();

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

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order o, CancellationToken cancellationToken) => o);

        mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        OrderDto result = await service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.CustomerId.Should().Be(1);
        mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Once);
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

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        Func<Task> act = async () => await service.CreateAsync(createDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        mockOrderRepository.Verify(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
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

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address?)null);

        // Act
        Func<Task> act = async () => await service.CreateAsync(createDto);

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

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        // Act
        Func<Task> act = async () => await service.CreateAsync(createDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateOrderDto? createDto = null;

        // Act
        Func<Task> act = async () => await service.CreateAsync(createDto!);

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

        mockOrderRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        IReadOnlyList<OrderDto> result = await service.GetByCustomerIdAsync(customerId);

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

        mockOrderRepository
            .Setup(r => r.GetByDateRangeAsync(startDate, endDate, It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        // Act
        IReadOnlyList<OrderDto> result = await service.GetByDateRangeAsync(startDate, endDate);

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

        mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<OrderDto> result = await service.UpdateAsync(orderId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(200.00m);
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int orderId = 999;
        UpdateOrderDto updateDto = new UpdateOrderDto { Total = 200.00m };

        mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        // Act
        Result<OrderDto> result = await service.UpdateAsync(orderId, updateDto);

        // Assert
        result.IsNotFound.Should().BeTrue();
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

        mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<OrderDto> result = await service.PatchAsync(orderId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Total.Should().Be(150.00m);
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

        mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        Result<OrderDto> result = await service.PatchAsync(orderId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
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

        mockOrderRepository
            .Setup(r => r.ExistsAsync(orderId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        mockOrderRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await service.DeleteAsync(orderId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int orderId = 999;

        mockOrderRepository
            .Setup(r => r.ExistsAsync(orderId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await service.DeleteAsync(orderId);

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

        mockOrderRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IReadOnlyList<OrderDto> result = await service.UpdateBatchAsync(updates);

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

        mockOrderRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IReadOnlyList<OrderDto> result = await service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Only 2 orders updated, 1 skipped
        mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
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

        mockOrderRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        mockOrderRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IReadOnlyList<int> result = await service.DeleteBatchAsync(ids);

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

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        Func<Task> act = async () => await service.CreateAsync(createDto);
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

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address?)null);

        // Act & Assert
        Func<Task> act = async () => await service.CreateAsync(createDto);
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

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        // Act & Assert
        Func<Task> act = async () => await service.CreateAsync(createDto);
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

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        mockOrderRepository
            .Setup(r => r.AddAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Order()));

        mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.CreateAsync(createDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int orderId = 1;
        UpdateOrderDto updateDto = new UpdateOrderDto { Total = 200.00m };
        Order existingOrder = new Order { Id = orderId, CustomerId = 1, Total = 100.00m };

        mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.UpdateAsync(orderId, updateDto);
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

        mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        // Act
        Result<OrderDto> result = await service.PatchAsync(orderId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockOrderRepository.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        mockOrderRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int orderId = 1;
        Order existingOrder = new Order { Id = orderId, CustomerId = 1, Total = 100.00m };

        mockOrderRepository
            .Setup(r => r.ExistsAsync(orderId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockOrderRepository
            .Setup(r => r.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingOrder);

        mockOrderRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockOrderRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.DeleteAsync(orderId);
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

        mockOrderRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        mockOrderRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.UpdateBatchAsync(updates);
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

        mockOrderRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        mockOrderRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.DeleteBatchAsync(ids);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

}
