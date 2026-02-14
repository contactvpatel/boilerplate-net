using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Business.Helpers;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for customer business operations.
/// </summary>
public class CustomerService(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ILogger<CustomerService> logger) : Interfaces.ICustomerService
{
    public async Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Customer? customer = await customerRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return customer?.Adapt<CustomerDto>();
    }

    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Customer> customers = await customerRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return customers.Adapt<IReadOnlyList<CustomerDto>>();
    }

    public async Task<(IReadOnlyList<CustomerDto> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<Customer> items, int totalCount) = await customerRepository
            .GetPagedAsync(pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<CustomerDto> customerDtos = items.Adapt<IReadOnlyList<CustomerDto>>();
        return (customerDtos, totalCount);
    }

    public async Task<CustomerDto> CreateAsync(CreateCustomerDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        logger.LogInformation("Creating new customer. FirstName: {FirstName}, LastName: {LastName}, Email: {Email}", createDto.FirstName, createDto.LastName, createDto.Email);
        Customer customer = createDto.Adapt<Customer>();
        await customerRepository.AddAsync(customer, cancellationToken).ConfigureAwait(false);
        await customerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Customer created successfully. CustomerId: {CustomerId}", customer.Id);
        return customer.Adapt<CustomerDto>();
    }

    public async Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        logger.LogInformation("Updating customer. CustomerId: {CustomerId}, FirstName: {FirstName}, LastName: {LastName}, Email: {Email}", id, updateDto.FirstName, updateDto.LastName, updateDto.Email);
        Customer? customer = await customerRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (customer == null)
        {
            return null;
        }

        updateDto.Adapt(customer);
        await customerRepository.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
        await customerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Customer updated successfully. CustomerId: {CustomerId}", id);
        return customer.Adapt<CustomerDto>();
    }

    public async Task<CustomerDto?> PatchAsync(int id, UpdateCustomerDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto);

        Customer? customer = await customerRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (customer == null)
        {
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
            await customerRepository.UpdateAsync(customer, cancellationToken).ConfigureAwait(false);
            await customerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Customer patched successfully. CustomerId: {CustomerId}", id);
        }

        return customer.Adapt<CustomerDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting customer. CustomerId: {CustomerId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await customerRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        // Check if already soft-deleted
        Customer? customer = await customerRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (customer == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            logger.LogInformation("Customer already deleted. CustomerId: {CustomerId}", id);
            return true;
        }

        // Perform soft delete
        await customerRepository.DeleteAsync(customer, cancellationToken).ConfigureAwait(false);
        await customerRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Customer deleted successfully. CustomerId: {CustomerId}", id);
        return true;
    }

    public async Task<CustomerDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        Customer? customer = await customerRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        return customer?.Adapt<CustomerDto>();
    }

    public async Task<IReadOnlyList<CustomerDto>> CreateBatchAsync(IReadOnlyList<CreateCustomerDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos);

        if (createDtos.Count == 0)
        {
            return Array.Empty<CustomerDto>();
        }

        logger.LogInformation("Creating {Count} customers in batch", createDtos.Count);
        List<Customer> customers = createDtos.Select(dto => dto.Adapt<Customer>()).ToList();

        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Customer customer in customers)
            {
                await customerRepository.AddAsync(customer, ct).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Batch created {Count} customers successfully", customers.Count);
        return customers.Adapt<IReadOnlyList<CustomerDto>>();
    }

    public async Task<IReadOnlyList<CustomerDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateCustomerDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        if (updates.Count == 0)
        {
            return Array.Empty<CustomerDto>();
        }

        logger.LogInformation("Updating {Count} customers in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Customer> customers = await customerRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Customer> customerLookup = customers.ToDictionary(c => c.Id);

        List<CustomerDto> updatedCustomers = new(updates.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach ((int id, UpdateCustomerDto updateDto) in updates)
            {
                if (customerLookup.TryGetValue(id, out Customer? customer))
                {
                    updateDto.Adapt(customer);
                    await customerRepository.UpdateAsync(customer, ct).ConfigureAwait(false);
                    updatedCustomers.Add(customer.Adapt<CustomerDto>());
                }
            }
        }, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Batch updated {Count} customers successfully", updatedCustomers.Count);
        return updatedCustomers;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        logger.LogInformation("Deleting {Count} customers in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Customer> customers = await customerRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(customers.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Customer customer in customers)
            {
                await customerRepository.DeleteAsync(customer, ct).ConfigureAwait(false);
                deletedIds.Add(customer.Id);
            }
        }, cancellationToken).ConfigureAwait(false);

        logger.LogInformation("Batch deleted {Count} customers successfully", deletedIds.Count);
        return deletedIds;
    }
}

