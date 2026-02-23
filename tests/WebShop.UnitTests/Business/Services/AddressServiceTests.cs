using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Business.DTOs;
using WebShop.Business.Models;
using WebShop.Business.Services;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.Business.Services;

/// <summary>
/// Unit tests for AddressService.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class AddressServiceTests
{
    private readonly Mock<IAddressRepository> mockAddressRepository = new();
    private readonly Mock<ICustomerRepository> mockCustomerRepository = new();
    private readonly Mock<IUnitOfWork> mockUnitOfWork = new();
    private readonly Mock<ILogger<AddressService>> mockLogger = new();
    private readonly AddressService service;

    public AddressServiceTests()
    {
        service = new AddressService(mockAddressRepository.Object, mockCustomerRepository.Object, mockUnitOfWork.Object, mockLogger.Object);
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

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        // Act
        Result<AddressDto> result = await service.GetByIdAsync(addressId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(addressId);
        result.Value.Address1.Should().Be("123 Main St");
        mockAddressRepository.Verify(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int addressId = 999;
        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address?)null);

        // Act
        Result<AddressDto> result = await service.GetByIdAsync(addressId);

        // Assert
        result.IsNotFound.Should().BeTrue();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_WhenCalled_ReturnsAllAddresses()
    {
        // Arrange
        List<Address> addresses = new List<Address>
        {
            new() { Id = 1, CustomerId = 1, Address1 = "123 Main St" },
            new() { Id = 2, CustomerId = 2, Address1 = "456 Oak Ave" }
        };

        mockAddressRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        // Act
        IReadOnlyList<AddressDto> result = await service.GetAllAsync();

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

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        mockAddressRepository
            .Setup(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address a, CancellationToken cancellationToken) => a);

        mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        AddressDto result = await service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Address1.Should().Be("123 Main St");
        mockAddressRepository.Verify(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Once);
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

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        Func<Task> act = async () => await service.CreateAsync(createDto);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        mockAddressRepository.Verify(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateAddressDto? createDto = null;

        // Act
        Func<Task> act = async () => await service.CreateAsync(createDto!);

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

        mockAddressRepository
            .Setup(r => r.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        // Act
        IReadOnlyList<AddressDto> result = await service.GetByCustomerIdAsync(customerId);

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

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        mockAddressRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<AddressDto> result = await service.UpdateAsync(addressId, updateDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address1.Should().Be("Updated Address");
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int addressId = 999;
        UpdateAddressDto updateDto = new UpdateAddressDto { Address1 = "Updated Address" };

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address?)null);

        // Act
        Result<AddressDto> result = await service.UpdateAsync(addressId, updateDto);

        // Assert
        result.IsNotFound.Should().BeTrue();
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

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act
        Func<Task> act = async () => await service.UpdateAsync(addressId, updateDto);

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

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        mockAddressRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<AddressDto> result = await service.PatchAsync(addressId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Address1.Should().Be("Patched Address");
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

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        // Act
        Result<AddressDto> result = await service.PatchAsync(addressId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockAddressRepository.Verify(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PatchAsync_ValidId_WithCustomerIdChange_PatchesAddress()
    {
        // Arrange - address has CustomerId 1, patch to CustomerId 2 (valid customer)
        const int addressId = 1;
        UpdateAddressDto patchDto = new UpdateAddressDto { CustomerId = 2 };

        Address existingAddress = new Address
        {
            Id = addressId,
            CustomerId = 1,
            Address1 = "123 Main St"
        };

        Customer customer2 = new Customer { Id = 2, FirstName = "Jane", LastName = "Doe", Email = "jane@example.com" };

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer2);

        mockAddressRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        Result<AddressDto> result = await service.PatchAsync(addressId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.CustomerId.Should().Be(2);
        mockAddressRepository.Verify(r => r.UpdateAsync(It.Is<Address>(a => a.CustomerId == 2), It.IsAny<CancellationToken>()), Times.Once);
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

        mockAddressRepository
            .Setup(r => r.ExistsAsync(addressId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(address);

        mockAddressRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await service.DeleteAsync(addressId);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int addressId = 999;

        mockAddressRepository
            .Setup(r => r.ExistsAsync(addressId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await service.DeleteAsync(addressId);

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

        mockCustomerRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { customer });

        mockAddressRepository
            .Setup(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Address a, CancellationToken cancellationToken) => a);

        mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<AddressDto> result = await service.CreateBatchAsync(createDtos);

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
        IReadOnlyList<AddressDto> result = await service.CreateBatchAsync(createDtos);

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

        mockAddressRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        mockAddressRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        IReadOnlyList<AddressDto> result = await service.UpdateBatchAsync(updates);

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

        mockAddressRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        mockAddressRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
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
        CreateAddressDto createDto = new CreateAddressDto
        {
            CustomerId = 999,
            Address1 = "Address",
            City = "City"
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

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        mockAddressRepository
            .Setup(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Address()));

        mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.CreateAsync(createDto);
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

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        Func<Task> act = async () => await service.UpdateAsync(addressId, updateDto);
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

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        mockAddressRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.UpdateAsync(addressId, updateDto);
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

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        mockCustomerRepository
            .Setup(r => r.GetByIdAsync(999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);

        // Act & Assert
        Func<Task> act = async () => await service.PatchAsync(addressId, patchDto);
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

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        // Act
        Result<AddressDto> result = await service.PatchAsync(addressId, patchDto);

        // Assert
        result.IsSuccess.Should().BeTrue();
        mockAddressRepository.Verify(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()), Times.Never);
        mockAddressRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int addressId = 1;
        Address existingAddress = new Address { Id = addressId, CustomerId = 1, Address1 = "Address" };

        mockAddressRepository
            .Setup(r => r.ExistsAsync(addressId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        mockAddressRepository
            .Setup(r => r.GetByIdAsync(addressId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAddress);

        mockAddressRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockAddressRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.DeleteAsync(addressId);
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

        mockCustomerRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>()); // No customers found for ID 999

        // Act & Assert
        Func<Task> act = async () => await service.CreateBatchAsync(createDtos);
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

        mockCustomerRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer> { customer });

        mockAddressRepository
            .Setup(r => r.AddAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.CreateBatchAsync(createDtos);
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

        mockAddressRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Address> { existingAddress });

        mockCustomerRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Customer>()); // No customers found for ID 999

        // Act
        IReadOnlyList<AddressDto> result = await service.UpdateBatchAsync(updates);

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

        mockAddressRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Address> { existingAddress });

        mockAddressRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
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
        List<Address> addresses = new List<Address>
        {
            new() { Id = 1, CustomerId = 1, Address1 = "Address" }
        };

        mockAddressRepository
            .Setup(r => r.FindByIdsAsync(It.IsAny<IReadOnlyList<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(addresses);

        mockAddressRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Address>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await service.DeleteBatchAsync(ids);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
