using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for address business operations.
/// </summary>
public class AddressService(
    IAddressRepository addressRepository,
    ICustomerRepository customerRepository,
    ILogger<AddressService> logger) : Interfaces.IAddressService
{
    private readonly IAddressRepository _addressRepository = addressRepository
        ?? throw new ArgumentNullException(nameof(addressRepository));
    private readonly ICustomerRepository _customerRepository = customerRepository
        ?? throw new ArgumentNullException(nameof(customerRepository));
    private readonly ILogger<AddressService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<AddressDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Address? address = await _addressRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return address?.Adapt<AddressDto>();
    }

    public async Task<IReadOnlyList<AddressDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Address> addresses = await _addressRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return addresses.Adapt<IReadOnlyList<AddressDto>>();
    }

    public async Task<AddressDto> CreateAsync(CreateAddressDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        // Validate customer exists
        Customer? customer = await _customerRepository.GetByIdAsync(createDto.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer == null)
        {
            _logger.LogWarning("Address creation failed: Customer not found. CustomerId: {CustomerId}", createDto.CustomerId);
            throw new ArgumentException($"Customer with ID {createDto.CustomerId} not found.", nameof(createDto));
        }

        _logger.LogInformation("Creating new address for customer. CustomerId: {CustomerId}, Address1: {Address1}, City: {City}, Zip: {Zip}", createDto.CustomerId, createDto.Address1, createDto.City, createDto.Zip);
        Address address = createDto.Adapt<Address>();
        await _addressRepository.AddAsync(address, cancellationToken).ConfigureAwait(false);
        await _addressRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Address created successfully. AddressId: {AddressId}", address.Id);
        return address.Adapt<AddressDto>();
    }

    public async Task<IReadOnlyList<AddressDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Address> addresses = await _addressRepository.GetByCustomerIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        return addresses.Adapt<IReadOnlyList<AddressDto>>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting address. AddressId: {AddressId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await _addressRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            // Never existed - return false (controller will return 404)
            _logger.LogWarning("Address not found for deletion. AddressId: {AddressId}", id);
            return false;
        }

        // Check if already soft-deleted
        Address? address = await _addressRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (address == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            _logger.LogInformation("Address already deleted. AddressId: {AddressId}", id);
            return true;
        }

        // Perform soft delete
        await _addressRepository.DeleteAsync(address, cancellationToken).ConfigureAwait(false);
        await _addressRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Address deleted successfully. AddressId: {AddressId}", id);
        return true;
    }

    public async Task<AddressDto?> UpdateAsync(int id, UpdateAddressDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        Address? address = await _addressRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (address == null)
        {
            _logger.LogWarning("Address not found for update. AddressId: {AddressId}", id);
            return null;
        }

        // Validate customer exists if CustomerId is being updated
        if (updateDto.CustomerId.HasValue && updateDto.CustomerId.Value != address.CustomerId)
        {
            Customer? customer = await _customerRepository.GetByIdAsync(updateDto.CustomerId.Value, cancellationToken).ConfigureAwait(false);
            if (customer == null)
            {
                _logger.LogWarning("Address update failed: Customer not found. CustomerId: {CustomerId}", updateDto.CustomerId.Value);
                throw new ArgumentException($"Customer with ID {updateDto.CustomerId.Value} not found.", nameof(updateDto));
            }
        }

        _logger.LogInformation("Updating address. AddressId: {AddressId}, CustomerId: {CustomerId}, City: {City}", id, updateDto.CustomerId, updateDto.City);

        // Full update: Map all provided fields (PUT operation - idempotent by nature)
        // TODO: Map update properties
        await _addressRepository.UpdateAsync(address, cancellationToken).ConfigureAwait(false);
        await _addressRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Address updated successfully. AddressId: {AddressId}", id);
        return address.Adapt<AddressDto>();
    }

    public async Task<AddressDto?> PatchAsync(int id, UpdateAddressDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto, nameof(patchDto));

        Address? address = await _addressRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (address == null)
        {
            _logger.LogWarning("Address not found for patch. AddressId: {AddressId}", id);
            return null;
        }

        // Partial update: Only update fields that are provided (not null)
        // This ensures idempotency - if values are already set, no change occurs
        bool hasChanges = false;

        if (patchDto.CustomerId.HasValue && address.CustomerId != patchDto.CustomerId.Value)
        {
            // Validate customer exists if CustomerId is being changed
            Customer? customer = await _customerRepository.GetByIdAsync(patchDto.CustomerId.Value, cancellationToken).ConfigureAwait(false);
            if (customer == null)
            {
                _logger.LogWarning("Address patch failed: Customer not found. CustomerId: {CustomerId}", patchDto.CustomerId.Value);
                throw new ArgumentException($"Customer with ID {patchDto.CustomerId.Value} not found.", nameof(patchDto));
            }

            address.CustomerId = patchDto.CustomerId.Value;
            hasChanges = true;
        }

        if (patchDto.FirstName != null && address.FirstName != patchDto.FirstName)
        {
            address.FirstName = patchDto.FirstName;
            hasChanges = true;
        }

        if (patchDto.LastName != null && address.LastName != patchDto.LastName)
        {
            address.LastName = patchDto.LastName;
            hasChanges = true;
        }

        if (patchDto.Address1 != null && address.Address1 != patchDto.Address1)
        {
            address.Address1 = patchDto.Address1;
            hasChanges = true;
        }

        if (patchDto.Address2 != null && address.Address2 != patchDto.Address2)
        {
            address.Address2 = patchDto.Address2;
            hasChanges = true;
        }

        if (patchDto.City != null && address.City != patchDto.City)
        {
            address.City = patchDto.City;
            hasChanges = true;
        }

        if (patchDto.Zip != null && address.Zip != patchDto.Zip)
        {
            address.Zip = patchDto.Zip;
            hasChanges = true;
        }

        // Save changes only if there were actual changes (idempotency: no-op if already in desired state)
        if (hasChanges)
        {
            await _addressRepository.UpdateAsync(address, cancellationToken).ConfigureAwait(false);
            await _addressRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Address patched successfully. AddressId: {AddressId}", id);
        }
        else
        {
            _logger.LogInformation("Address patch completed with no changes. AddressId: {AddressId}", id);
        }

        return address.Adapt<AddressDto>();
    }

    public async Task<IReadOnlyList<AddressDto>> CreateBatchAsync(IReadOnlyList<CreateAddressDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos, nameof(createDtos));

        if (createDtos.Count == 0)
        {
            return Array.Empty<AddressDto>();
        }

        _logger.LogInformation("Creating {Count} addresses in batch", createDtos.Count);
        List<Address> addresses = createDtos.Select(dto => dto.Adapt<Address>()).ToList();

        foreach (Address address in addresses)
        {
            // Validate customer exists
            Customer? customer = await _customerRepository.GetByIdAsync(address.CustomerId ?? 0, cancellationToken).ConfigureAwait(false);
            if (customer == null && address.CustomerId.HasValue)
            {
                _logger.LogWarning("Address creation failed: Customer not found. CustomerId: {CustomerId}", address.CustomerId);
                throw new ArgumentException($"Customer with ID {address.CustomerId} not found.", nameof(createDtos));
            }

            await _addressRepository.AddAsync(address, cancellationToken).ConfigureAwait(false);
        }

        await _addressRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch created {Count} addresses successfully", addresses.Count);
        return addresses.Adapt<IReadOnlyList<AddressDto>>();
    }

    public async Task<IReadOnlyList<AddressDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateAddressDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates, nameof(updates));

        if (updates.Count == 0)
        {
            return Array.Empty<AddressDto>();
        }

        _logger.LogInformation("Updating {Count} addresses in batch", updates.Count);

        // Load all addresses in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Address> addresses = await _addressRepository.FindAsync(a => ids.Contains(a.Id), cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Address> addressLookup = addresses.ToDictionary(a => a.Id);

        // Load all unique CustomerIds that need validation in a single query
        IReadOnlyList<int> customerIds = updates
            .Where(u => u.UpdateDto.CustomerId.HasValue)
            .Select(u => u.UpdateDto.CustomerId!.Value)
            .Distinct()
            .ToList();

        Dictionary<int, Customer> customerLookup = new();
        if (customerIds.Count > 0)
        {
            IReadOnlyList<Customer> customers = await _customerRepository.FindAsync(c => customerIds.Contains(c.Id), cancellationToken).ConfigureAwait(false);
            customerLookup = customers.ToDictionary(c => c.Id);
        }

        List<AddressDto> updatedAddresses = new(updates.Count);
        foreach ((int id, UpdateAddressDto updateDto) in updates)
        {
            if (!addressLookup.TryGetValue(id, out Address? address))
            {
                continue;
            }

            // Validate CustomerId if provided
            if (updateDto.CustomerId.HasValue)
            {
                if (!customerLookup.ContainsKey(updateDto.CustomerId.Value))
                {
                    _logger.LogWarning("Address batch update skipped: Customer not found. AddressId: {AddressId}, CustomerId: {CustomerId}", id, updateDto.CustomerId.Value);
                    continue;
                }
            }

            // TODO: Map update properties
            await _addressRepository.UpdateAsync(address, cancellationToken).ConfigureAwait(false);
            updatedAddresses.Add(address.Adapt<AddressDto>());
        }

        await _addressRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch updated {Count} addresses successfully", updatedAddresses.Count);
        return updatedAddresses;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        _logger.LogInformation("Deleting {Count} addresses in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Address> addresses = await _addressRepository.FindAsync(a => ids.Contains(a.Id), cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(addresses.Count);
        foreach (Address address in addresses)
        {
            await _addressRepository.DeleteAsync(address, cancellationToken).ConfigureAwait(false);
            deletedIds.Add(address.Id);
        }

        await _addressRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch deleted {Count} addresses successfully", deletedIds.Count);
        return deletedIds;
    }
}

