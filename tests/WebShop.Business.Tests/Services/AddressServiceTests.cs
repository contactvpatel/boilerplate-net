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
/// Unit tests for AddressService.
/// </summary>
[Trait("Category", "Unit")]
public class AddressServiceTests
{
    private readonly Mock<IAddressRepository> _mockAddressRepository;
    private readonly Mock<ICustomerRepository> _mockCustomerRepository;
    private readonly Mock<ILogger<AddressService>> _mockLogger;
    private readonly AddressService _service;

    public AddressServiceTests()
    {
        _mockAddressRepository = new Mock<IAddressRepository>();
        _mockCustomerRepository = new Mock<ICustomerRepository>();
        _mockLogger = new Mock<ILogger<AddressService>>();
        _service = new AddressService(_mockAddressRepository.Object, _mockCustomerRepository.Object, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsAddressDto()
    {
        // Arrange
        const int addressId = 1;
        Address address = new Address
        {
            Id = addressId,
            CustomerId = 1,
            Address1 = "123 Main St",
            City = "New York",
            Zip = "10001"
        };

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        // Act
        AddressDto? result = await _service.GetByIdAsync(addressId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(addressId);
        result.Address1.Should().Be("123 Main St");
        _mockAddressRepository.Verify(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int addressId = 999;
        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address?)null);

        // Act
        AddressDto? result = await _service.GetByIdAsync(addressId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllAddresses()
    {
        // Arrange
        List<Address> addresses = new List<Address>
        {
            new() { Id = 1, CustomerId = 1, Address1 = "123 Main St" },
            new() { Id = 2, CustomerId = 2, Address1 = "456 Oak Ave" }
        };

        _mockAddressRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        // Act
        IReadOnlyList<AddressDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesAddress()
    {
        // Arrange
        CreateAddressDto createDto = new CreateAddressDto
        {
            CustomerId = 1,
            Address1 = "123 Main St",
            City = "New York",
            Zip = "10001"
        };

        Customer customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };
        Address address = new Address
        {
            Id = 1,
            CustomerId = 1,
            Address1 = "123 Main St",
            City = "New York",
            Zip = "10001"
        };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockAddressRepository
            .Setup(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address a, CancellationToken cancellationToken) => a);

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        AddressDto result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Address1.Should().Be("123 Main St");
        _mockAddressRepository.Verify(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_InvalidCustomerId_ThrowsArgumentException()
    {
        // Arrange
        CreateAddressDto createDto = new CreateAddressDto
        {
            CustomerId = 999,
            Address1 = "123 Main St"
        };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        Func<Task> act = async () => await _service.CreateAsync(createDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _mockAddressRepository.Verify(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateAddressDto? createDto = null;

        // Act
        Func<Task> act = async () => await _service.CreateAsync(createDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region GetByCustomerIdAsync Tests

    [Fact]
    public async Task GetByCustomerIdAsync_ValidCustomerId_ReturnsAddresses()
    {
        // Arrange
        const int customerId = 1;
        List<Address> addresses = new List<Address>
        {
            new() { Id = 1, CustomerId = customerId, Address1 = "123 Main St" },
            new() { Id = 2, CustomerId = customerId, Address1 = "456 Oak Ave" }
        };

        _mockAddressRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        // Act
        IReadOnlyList<AddressDto> result = await _service.GetByCustomerIdAsync(customerId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidId_UpdatesAddress()
    {
        // Arrange
        const int addressId = 1;
        UpdateAddressDto updateDto = new UpdateAddressDto
        {
            Address1 = "Updated Address",
            City = "Updated City"
        };

        Address existingAddress = new Address
        {
            Id = addressId,
            CustomerId = 1,
            Address1 = "Original Address",
            City = "Original City"
        };

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        _mockAddressRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        AddressDto? result = await _service.UpdateAsync(addressId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Address1.Should().Be("Updated Address");
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int addressId = 999;
        UpdateAddressDto updateDto = new UpdateAddressDto { Address1 = "Updated Address" };

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address?)null);

        // Act
        AddressDto? result = await _service.UpdateAsync(addressId, updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_InvalidCustomerId_ThrowsArgumentException()
    {
        // Arrange
        const int addressId = 1;
        UpdateAddressDto updateDto = new UpdateAddressDto
        {
            CustomerId = 999,
            Address1 = "Updated Address"
        };

        Address existingAddress = new Address
        {
            Id = addressId,
            CustomerId = 1,
            Address1 = "Original Address"
        };

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(addressId, updateDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region PatchAsync Tests

    [Fact]
    public async Task PatchAsync_ValidId_WithChanges_PatchesAddress()
    {
        // Arrange
        const int addressId = 1;
        UpdateAddressDto patchDto = new UpdateAddressDto
        {
            Address1 = "Patched Address"
        };

        Address existingAddress = new Address
        {
            Id = addressId,
            CustomerId = 1,
            Address1 = "Original Address"
        };

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        _mockAddressRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        AddressDto? result = await _service.PatchAsync(addressId, patchDto);

        // Assert
        result.Should().NotBeNull();
        result!.Address1.Should().Be("Patched Address");
    }

    [Fact]
    public async Task PatchAsync_ValidId_NoChanges_ReturnsAddressWithoutSaving()
    {
        // Arrange
        const int addressId = 1;
        UpdateAddressDto patchDto = new UpdateAddressDto
        {
            Address1 = "Original Address" // Same as existing
        };

        Address existingAddress = new Address
        {
            Id = addressId,
            CustomerId = 1,
            Address1 = "Original Address"
        };

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        // Act
        AddressDto? result = await _service.PatchAsync(addressId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockAddressRepository.Verify(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesAddress()
    {
        // Arrange
        const int addressId = 1;
        Address address = new Address
        {
            Id = addressId,
            CustomerId = 1,
            Address1 = "123 Main St"
        };

        _mockAddressRepository
            .Setup(r => r.ExistsAsync(addressId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        _mockAddressRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await _service.DeleteAsync(addressId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int addressId = 999;

        _mockAddressRepository
            .Setup(r => r.ExistsAsync(addressId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.DeleteAsync(addressId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Batch Operations Tests

    [Fact]
    public async Task CreateBatchAsync_ValidDtos_CreatesAddresses()
    {
        // Arrange
        List<CreateAddressDto> createDtos = new List<CreateAddressDto>
        {
            new() { CustomerId = 1, Address1 = "Address 1" },
            new() { CustomerId = 1, Address1 = "Address 2" }
        };

        Customer customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockAddressRepository
            .Setup(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address a, CancellationToken cancellationToken) => a);

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<AddressDto> result = await _service.CreateBatchAsync(createDtos);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateBatchAsync_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        List<CreateAddressDto> createDtos = new List<CreateAddressDto>();

        // Act
        IReadOnlyList<AddressDto> result = await _service.CreateBatchAsync(createDtos);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBatchAsync_ValidUpdates_UpdatesAddresses()
    {
        // Arrange
        List<(int Id, UpdateAddressDto UpdateDto)> updates = new List<(int, UpdateAddressDto)>
        {
            (1, new UpdateAddressDto { Address1 = "Updated 1" }),
            (2, new UpdateAddressDto { Address1 = "Updated 2" })
        };

        List<Address> addresses = new List<Address>
        {
            new() { Id = 1, CustomerId = 1, Address1 = "Original 1" },
            new() { Id = 2, CustomerId = 1, Address1 = "Original 2" }
        };

        _mockAddressRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Address, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        _mockAddressRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<AddressDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task DeleteBatchAsync_ValidIds_DeletesAddresses()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2 };
        List<Address> addresses = new List<Address>
        {
            new() { Id = 1, CustomerId = 1, Address1 = "Address 1" },
            new() { Id = 2, CustomerId = 1, Address1 = "Address 2" }
        };

        _mockAddressRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Address, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        _mockAddressRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAddressRepository
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
        CreateAddressDto createDto = new CreateAddressDto
        {
            CustomerId = 999,
            Address1 = "Address",
            City = "City"
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
    public async Task CreateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        CreateAddressDto createDto = new CreateAddressDto
        {
            CustomerId = 1,
            Address1 = "Address",
            City = "City"
        };

        Customer customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockAddressRepository
            .Setup(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Address()));

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.CreateAsync(createDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateAsync_CustomerNotFound_ThrowsArgumentException()
    {
        // Arrange
        const int addressId = 1;
        UpdateAddressDto updateDto = new UpdateAddressDto
        {
            CustomerId = 999,
            Address1 = "Updated Address"
        };

        Address existingAddress = new Address
        {
            Id = addressId,
            CustomerId = 1,
            Address1 = "Original Address"
        };

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        Func<Task> act = async () => await _service.UpdateAsync(addressId, updateDto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Customer with ID 999 not found*");
    }

    [Fact]
    public async Task UpdateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int addressId = 1;
        UpdateAddressDto updateDto = new UpdateAddressDto { Address1 = "Updated Address" };
        Address existingAddress = new Address { Id = addressId, CustomerId = 1, Address1 = "Original Address" };

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        _mockAddressRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.UpdateAsync(addressId, updateDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PatchAsync_CustomerNotFound_ThrowsArgumentException()
    {
        // Arrange
        const int addressId = 1;
        UpdateAddressDto patchDto = new UpdateAddressDto
        {
            CustomerId = 999
        };

        Address existingAddress = new Address
        {
            Id = addressId,
            CustomerId = 1,
            Address1 = "Address"
        };

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        Func<Task> act = async () => await _service.PatchAsync(addressId, patchDto);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Customer with ID 999 not found*");
    }

    [Fact]
    public async Task PatchAsync_NoChanges_DoesNotCallUpdate()
    {
        // Arrange
        const int addressId = 1;
        Address existingAddress = new Address
        {
            Id = addressId,
            CustomerId = 1,
            Address1 = "Address",
            City = "City"
        };

        UpdateAddressDto patchDto = new UpdateAddressDto
        {
            Address1 = "Address", // Same value
            City = "City" // Same value
        };

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        // Act
        AddressDto? result = await _service.PatchAsync(addressId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockAddressRepository.Verify(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockAddressRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int addressId = 1;
        Address existingAddress = new Address { Id = addressId, CustomerId = 1, Address1 = "Address" };

        _mockAddressRepository
            .Setup(r => r.ExistsAsync(addressId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        _mockAddressRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteAsync(addressId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateBatchAsync_CustomerNotFound_ThrowsArgumentException()
    {
        // Arrange
        List<CreateAddressDto> createDtos = new List<CreateAddressDto>
        {
            new() { CustomerId = 999, Address1 = "Address", City = "City" }
        };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        Func<Task> act = async () => await _service.CreateBatchAsync(createDtos);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Customer with ID 999 not found*");
    }

    [Fact]
    public async Task CreateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<CreateAddressDto> createDtos = new List<CreateAddressDto>
        {
            new() { CustomerId = 1, Address1 = "Address", City = "City" }
        };

        Customer customer = new Customer { Id = 1, FirstName = "John", LastName = "Doe", Email = "john@example.com" };

        _mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        _mockAddressRepository
            .Setup(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Address()));

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.CreateBatchAsync(createDtos);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task UpdateBatchAsync_CustomerNotFound_SkipsAddress()
    {
        // Arrange
        List<(int Id, UpdateAddressDto UpdateDto)> updates = new List<(int, UpdateAddressDto)>
        {
            (1, new UpdateAddressDto { CustomerId = 999, Address1 = "Updated" })
        };

        Address existingAddress = new Address
        {
            Id = 1,
            CustomerId = 1,
            Address1 = "Original"
        };

        _mockAddressRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Address, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Address> { existingAddress });

        _mockCustomerRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Customer, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>()); // No customers found

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        IReadOnlyList<AddressDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty(); // Address skipped due to invalid customer
    }

    [Fact]
    public async Task UpdateBatchAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        List<(int Id, UpdateAddressDto UpdateDto)> updates = new List<(int, UpdateAddressDto)>
        {
            (1, new UpdateAddressDto { Address1 = "Updated" })
        };

        Address existingAddress = new Address
        {
            Id = 1,
            CustomerId = 1,
            Address1 = "Original"
        };

        _mockAddressRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Address, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Address> { existingAddress });

        _mockAddressRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAddressRepository
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
        List<Address> addresses = new List<Address>
        {
            new() { Id = 1, CustomerId = 1, Address1 = "Address" }
        };

        _mockAddressRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Address, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        _mockAddressRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteBatchAsync(ids);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
