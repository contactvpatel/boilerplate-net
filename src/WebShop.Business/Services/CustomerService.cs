using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for customer business operations.
/// </summary>
public class CustomerService(ICustomerRepository customerRepository, ILogger<CustomerService> logger) : Interfaces.ICustomerService
{
    private readonly ICustomerRepository _customerRepository = customerRepository
        ?? throw new ArgumentNullException(nameof(customerRepository));
    private readonly ILogger<CustomerService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Customer? customer = await _customerRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return customer?.Adapt<CustomerDto>();
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Customer> customers = await _customerRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return customers.Adapt<IReadOnlyList<CustomerDto>>();
    }

    public async Task<(IReadOnlyList<CustomerDto> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<Customer> items, int totalCount) = await _customerRepository
            .GetPagedAsync(pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CustomerDto> customerDtos = items.Adapt<IReadOnlyList<CustomerDto>>();
        return (customerDtos, totalCount);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        _logger.LogInformation("Creating new customer. FirstName: {FirstName}, LastName: {LastName}, Email: {Email}", createDto.FirstName, createDto.LastName, createDto.Email);
        Customer customer = createDto.Adapt<Customer>();
        await _customerRepository.AddAsync(customer, cancellationToken).ConfigureAwait(false);
        await _customerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Customer created successfully. CustomerId: {CustomerId}", customer.Id);
        return customer.Adapt<CustomerDto>();
    }

    public async Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        _logger.LogInformation("Updating customer. CustomerId: {CustomerId}, FirstName: {FirstName}, LastName: {LastName}, Email: {Email}", id, updateDto.FirstName, updateDto.LastName, updateDto.Email);
        Customer? customer = await _customerRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (customer == null)
        {
            _logger.LogWarning("Customer not found for update. CustomerId: {CustomerId}", id);
            return null;
        }

        // TODO: Map update properties
        await _customerRepository.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
        await _customerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Customer updated successfully. CustomerId: {CustomerId}", id);
        return customer.Adapt<CustomerDto>();
    }

    public async Task<CustomerDto?> PatchAsync(int id, UpdateCustomerDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto, nameof(patchDto));

        Customer? customer = await _customerRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (customer == null)
        {
            _logger.LogWarning("Customer not found for patch. CustomerId: {CustomerId}", id);
            return null;
        }

        // Partial update: Only update fields that are provided (not null)
        bool hasChanges = false;

        if (patchDto.FirstName != null && customer.FirstName != patchDto.FirstName)
        {
            customer.FirstName = patchDto.FirstName;
            hasChanges = true;
        }

        if (patchDto.LastName != null && customer.LastName != patchDto.LastName)
        {
            customer.LastName = patchDto.LastName;
            hasChanges = true;
        }

        if (patchDto.Email != null && customer.Email != patchDto.Email)
        {
            customer.Email = patchDto.Email;
            hasChanges = true;
        }

        if (patchDto.Gender != null && customer.Gender != patchDto.Gender)
        {
            customer.Gender = patchDto.Gender;
            hasChanges = true;
        }

        if (patchDto.DateOfBirth.HasValue && customer.DateOfBirth != patchDto.DateOfBirth.Value)
        {
            customer.DateOfBirth = patchDto.DateOfBirth.Value;
            hasChanges = true;
        }

        if (patchDto.CurrentAddressId.HasValue && customer.CurrentAddressId != patchDto.CurrentAddressId.Value)
        {
            customer.CurrentAddressId = patchDto.CurrentAddressId.Value;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _customerRepository.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await _customerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Customer patched successfully. CustomerId: {CustomerId}", id);
        }

        return customer.Adapt<CustomerDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting customer. CustomerId: {CustomerId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await _customerRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            // Never existed - return false (controller will return 404)
            _logger.LogWarning("Customer not found for deletion. CustomerId: {CustomerId}", id);
            return false;
        }

        // Check if already soft-deleted
        Customer? customer = await _customerRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (customer == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            _logger.LogInformation("Customer already deleted. CustomerId: {CustomerId}", id);
            return true;
        }

        // Perform soft delete
        await _customerRepository.DeleteAsync(customer, cancellationToken).ConfigureAwait(false);
        await _customerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Customer deleted successfully. CustomerId: {CustomerId}", id);
        return true;
    }

    public async Task<CustomerDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        Customer? customer = await _customerRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        return customer?.Adapt<CustomerDto>();
    }

    public async Task<IReadOnlyList<CustomerDto>> CreateBatchAsync(IReadOnlyList<CreateCustomerDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos, nameof(createDtos));

        if (createDtos.Count == 0)
        {
            return Array.Empty<CustomerDto>();
        }

        _logger.LogInformation("Creating {Count} customers in batch", createDtos.Count);
        List<Customer> customers = createDtos.Select(dto => dto.Adapt<Customer>()).ToList();

        foreach (Customer customer in customers)
        {
            await _customerRepository.AddAsync(customer, cancellationToken).ConfigureAwait(false);
        }

        await _customerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch created {Count} customers successfully", customers.Count);
        return customers.Adapt<IReadOnlyList<CustomerDto>>();
    }

    public async Task<IReadOnlyList<CustomerDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateCustomerDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates, nameof(updates));

        if (updates.Count == 0)
        {
            return Array.Empty<CustomerDto>();
        }

        _logger.LogInformation("Updating {Count} customers in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Customer> customers = await _customerRepository.FindAsync(c => ids.Contains(c.Id), cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Customer> customerLookup = customers.ToDictionary(c => c.Id);

        List<CustomerDto> updatedCustomers = new(updates.Count);
        foreach ((int id, UpdateCustomerDto updateDto) in updates)
        {
            if (customerLookup.TryGetValue(id, out Customer? customer))
            {
                // TODO: Map update properties
                await _customerRepository.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
                updatedCustomers.Add(customer.Adapt<CustomerDto>());
            }
        }

        await _customerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch updated {Count} customers successfully", updatedCustomers.Count);
        return updatedCustomers;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        _logger.LogInformation("Deleting {Count} customers in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Customer> customers = await _customerRepository.FindAsync(c => ids.Contains(c.Id), cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(customers.Count);
        foreach (Customer customer in customers)
        {
            await _customerRepository.DeleteAsync(customer, cancellationToken).ConfigureAwait(false);
            deletedIds.Add(customer.Id);
        }

        await _customerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch deleted {Count} customers successfully", deletedIds.Count);
        return deletedIds;
    }
}

