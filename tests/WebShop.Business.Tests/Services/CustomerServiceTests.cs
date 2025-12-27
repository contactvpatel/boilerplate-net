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
/// Unit tests for CustomerService.
/// </summary>
[Trait("Category", "Unit")]
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

    #region GetByIdAsync Tests

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
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john.doe@example.com");
        _mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int customerId = 999;
        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        CustomerDto? result = await _service.GetByIdAsync(customerId);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllCustomers()
    {
        // Arrange
        List<Customer> customers = new List<Customer>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        // Act
        IReadOnlyList<CustomerDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        _mockRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_NoCustomers_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Customer>());

        // Act
        IReadOnlyList<CustomerDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesCustomer()
    {
        // Arrange
        CreateCustomerDto createDto = new CreateCustomerDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        Customer customer = new Customer
        {
            Id = 1,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken ct) => c);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        CustomerDto result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.FirstName.Should().Be("John");
        result.LastName.Should().Be("Doe");
        result.Email.Should().Be("john.doe@example.com");
        _mockRepository.Verify(r => r.AddAsync(It.Is<Customer>(c => c.Email == "john.doe@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

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

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidId_UpdatesCustomer()
    {
        // Arrange
        const int customerId = 1;
        UpdateCustomerDto updateDto = new UpdateCustomerDto
        {
            FirstName = "John Updated",
            LastName = "Doe Updated",
            Email = "john.updated@example.com"
        };

        Customer existingCustomer = new Customer
        {
            Id = customerId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        CustomerDto? result = await _service.UpdateAsync(customerId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John Updated");
        result.LastName.Should().Be("Doe Updated");
        result.Email.Should().Be("john.updated@example.com");
        _mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int customerId = 999;
        UpdateCustomerDto updateDto = new UpdateCustomerDto
        {
            FirstName = "John Updated",
            LastName = "Doe Updated",
            Email = "john.updated@example.com"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        CustomerDto? result = await _service.UpdateAsync(customerId, updateDto);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        const int customerId = 1;
        UpdateCustomerDto? updateDto = null;

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(customerId, updateDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesCustomer()
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
            .Setup(r => r.ExistsAsync(customerId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await _service.DeleteAsync(customerId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.ExistsAsync(customerId, true, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int customerId = 999;

        _mockRepository
            .Setup(r => r.ExistsAsync(customerId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.DeleteAsync(customerId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.ExistsAsync(customerId, true, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeleted_ReturnsTrue()
    {
        // Arrange
        const int customerId = 1;

        _mockRepository
            .Setup(r => r.ExistsAsync(customerId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null); // Already soft-deleted

        // Act
        bool result = await _service.DeleteAsync(customerId);

        // Assert
        result.Should().BeTrue(); // Idempotent - returns true even if already deleted
        _mockRepository.Verify(r => r.ExistsAsync(customerId, true, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region PatchAsync Tests

    [Fact]
    public async Task PatchAsync_ValidId_WithChanges_PatchesCustomer()
    {
        // Arrange
        const int customerId = 1;
        UpdateCustomerDto patchDto = new UpdateCustomerDto
        {
            FirstName = "John Updated",
            Email = "john.updated@example.com"
        };

        Customer existingCustomer = new Customer
        {
            Id = customerId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        CustomerDto? result = await _service.PatchAsync(customerId, patchDto);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John Updated");
        result.Email.Should().Be("john.updated@example.com");
        _mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_ValidId_NoChanges_ReturnsCustomerWithoutSaving()
    {
        // Arrange
        const int customerId = 1;
        UpdateCustomerDto patchDto = new UpdateCustomerDto
        {
            FirstName = "John", // Same as existing
            Email = "john.doe@example.com" // Same as existing
        };

        Customer existingCustomer = new Customer
        {
            Id = customerId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        // Act
        CustomerDto? result = await _service.PatchAsync(customerId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PatchAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int customerId = 999;
        UpdateCustomerDto patchDto = new UpdateCustomerDto
        {
            FirstName = "John Updated"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        CustomerDto? result = await _service.PatchAsync(customerId, patchDto);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PatchAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        const int customerId = 1;
        UpdateCustomerDto? patchDto = null;

        // Act
        Func<Task> act = async () => await _service.PatchAsync(customerId, patchDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task PatchAsync_PartialFields_OnlyUpdatesProvidedFields()
    {
        // Arrange
        const int customerId = 1;
        UpdateCustomerDto patchDto = new UpdateCustomerDto
        {
            FirstName = "John Updated"
            // Other fields are null - should not be updated
        };

        Customer existingCustomer = new Customer
        {
            Id = customerId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com",
            Gender = "Male"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        CustomerDto? result = await _service.PatchAsync(customerId, patchDto);

        // Assert
        result.Should().NotBeNull();
        result!.FirstName.Should().Be("John Updated");
        result.LastName.Should().Be("Doe"); // Should remain unchanged
        result.Email.Should().Be("john.doe@example.com"); // Should remain unchanged
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Batch Operations Tests

    [Fact]
    public async Task CreateBatchAsync_ValidDtos_CreatesCustomers()
    {
        // Arrange
        List<CreateCustomerDto> createDtos = new List<CreateCustomerDto>
        {
            new() { FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            new() { FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer c, CancellationToken ct) => c);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<CustomerDto> result = await _service.CreateBatchAsync(createDtos);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBatchAsync_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        List<CreateCustomerDto> createDtos = new List<CreateCustomerDto>();

        // Act
        IReadOnlyList<CustomerDto> result = await _service.CreateBatchAsync(createDtos);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBatchAsync_NullDtos_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<CreateCustomerDto>? createDtos = null;

        // Act
        Func<Task> act = async () => await _service.CreateBatchAsync(createDtos!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateBatchAsync_ValidUpdates_UpdatesCustomers()
    {
        // Arrange
        List<(int Id, UpdateCustomerDto UpdateDto)> updates = new List<(int, UpdateCustomerDto)>
        {
            (1, new UpdateCustomerDto { FirstName = "John Updated" }),
            (2, new UpdateCustomerDto { FirstName = "Jane Updated" })
        };

        List<Customer> customers = new List<Customer>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<CustomerDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateBatchAsync_SomeCustomersNotFound_SkipsMissingCustomers()
    {
        // Arrange
        List<(int Id, UpdateCustomerDto UpdateDto)> updates = new List<(int, UpdateCustomerDto)>
        {
            (1, new UpdateCustomerDto { FirstName = "John Updated" }),
            (2, new UpdateCustomerDto { FirstName = "Jane Updated" }),
            (999, new UpdateCustomerDto { FirstName = "Missing Updated" }) // Not found
        };

        List<Customer> customers = new List<Customer>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
            // Customer 999 is missing
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<CustomerDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Only 2 customers updated, 1 skipped
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateBatchAsync_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        List<(int Id, UpdateCustomerDto UpdateDto)> updates = new List<(int, UpdateCustomerDto)>();

        // Act
        IReadOnlyList<CustomerDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateBatchAsync_NullUpdates_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<(int Id, UpdateCustomerDto UpdateDto)>? updates = null;

        // Act
        Func<Task> act = async () => await _service.UpdateBatchAsync(updates!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteBatchAsync_ValidIds_DeletesCustomers()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2 };
        List<Customer> customers = new List<Customer>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" },
            new() { Id = 2, FirstName = "Jane", LastName = "Smith", Email = "jane@example.com" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<int> result = await _service.DeleteBatchAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(1);
        result.Should().Contain(2);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task DeleteBatchAsync_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        List<int> ids = new List<int>();

        // Act
        IReadOnlyList<int> result = await _service.DeleteBatchAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteBatchAsync_NullIds_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<int>? ids = null;

        // Act
        Func<Task> act = async () => await _service.DeleteBatchAsync(ids!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        CreateCustomerDto createDto = new CreateCustomerDto
        {
            FirstName = "John",
            LastName = "Doe",
            Email = "john.doe@example.com"
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Customer()));

        _mockRepository
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
        const int customerId = 1;
        UpdateCustomerDto updateDto = new UpdateCustomerDto { FirstName = "Updated John" };
        Customer existingCustomer = new Customer { Id = customerId, FirstName = "John", LastName = "Doe", Email = "john@example.com" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.UpdateAsync(customerId, updateDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PatchAsync_NoChanges_DoesNotCallUpdate()
    {
        // Arrange
        const int customerId = 1;
        Customer existingCustomer = new Customer
        {
            Id = customerId,
            FirstName = "John",
            LastName = "Doe",
            Email = "john@example.com",
            Gender = "Male",
            DateOfBirth = new DateTime(1990, 1, 1),
            CurrentAddressId = 1
        };

        UpdateCustomerDto patchDto = new UpdateCustomerDto
        {
            FirstName = "John", // Same value
            LastName = "Doe", // Same value
            Email = "john@example.com" // Same value
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        // Act
        CustomerDto? result = await _service.PatchAsync(customerId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int customerId = 1;
        Customer existingCustomer = new Customer { Id = customerId, FirstName = "John", LastName = "Doe", Email = "john@example.com" };

        _mockRepository
            .Setup(r => r.ExistsAsync(customerId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingCustomer);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteAsync(customerId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<CreateCustomerDto> createDtos = new List<CreateCustomerDto>
        {
            new() { FirstName = "John", LastName = "Doe", Email = "john@example.com" }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Customer()));

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.CreateBatchAsync(createDtos);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<(int Id, UpdateCustomerDto UpdateDto)> updates = new List<(int, UpdateCustomerDto)>
        {
            (1, new UpdateCustomerDto { FirstName = "Updated John" })
        };

        List<Customer> customers = new List<Customer>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
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
        List<Customer> customers = new List<Customer>
        {
            new() { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customers);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteBatchAsync(ids);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion

}
