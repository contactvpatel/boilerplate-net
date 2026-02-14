using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Business.Helpers;
using WebShop.Business.Models;
using WebShop.Business.Services.Base;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for address business operations.
/// </summary>
public class AddressService(
    IAddressRepository addressRepository,
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ILogger<AddressService> logger)
    : CrudServiceBase<Address, AddressDto, CreateAddressDto, UpdateAddressDto>(addressRepository, unitOfWork, logger),
        Interfaces.IAddressService
{
    private readonly IAddressRepository _addressRepository = addressRepository;
    private readonly ICustomerRepository _customerRepository = customerRepository;

    /// <inheritdoc />
    protected override string EntityName => "Address";

    /// <inheritdoc />
    public async Task<IReadOnlyList<AddressDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default)
    {
        List<Address> addresses = await _addressRepository.GetByCustomerIdAsync(customerId, cancellationToken).ConfigureAwait(false);
        return addresses.Adapt<IReadOnlyList<AddressDto>>();
    }

    /// <inheritdoc />
    public override async Task<AddressDto> CreateAsync(CreateAddressDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        Customer? customer = await _customerRepository.GetByIdAsync(createDto.CustomerId, cancellationToken).ConfigureAwait(false);
        if (customer == null)
        {
            Logger.LogWarning("Address creation failed: Customer not found. CustomerId: {CustomerId}", createDto.CustomerId);
            throw new ArgumentException($"Customer with ID {createDto.CustomerId} not found.", nameof(createDto));
        }

        Logger.LogInformation("Creating new address for customer. CustomerId: {CustomerId}, Address1: {Address1}, City: {City}, Zip: {Zip}", createDto.CustomerId, createDto.Address1, createDto.City, createDto.Zip);
        return await base.CreateAsync(createDto, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<Result<AddressDto>> UpdateAsync(int id, UpdateAddressDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        Address? address = await Repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (address == null)
        {
            return Result<AddressDto>.NotFound();
        }

        if (updateDto.CustomerId.HasValue && updateDto.CustomerId.Value != address.CustomerId)
        {
            Customer? customer = await _customerRepository.GetByIdAsync(updateDto.CustomerId.Value, cancellationToken).ConfigureAwait(false);
            if (customer == null)
            {
                Logger.LogWarning("Address update failed: Customer not found. CustomerId: {CustomerId}", updateDto.CustomerId.Value);
                throw new ArgumentException($"Customer with ID {updateDto.CustomerId.Value} not found.", nameof(updateDto));
            }
        }

        Logger.LogInformation("Updating address. AddressId: {AddressId}, CustomerId: {CustomerId}, City: {City}", id, updateDto.CustomerId, updateDto.City);
        return await base.UpdateAsync(id, updateDto, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<Result<AddressDto>> PatchAsync(int id, UpdateAddressDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto);

        Address? address = await Repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (address == null)
        {
            return Result<AddressDto>.NotFound();
        }

        if (patchDto.CustomerId.HasValue && address.CustomerId != patchDto.CustomerId.Value)
        {
            Customer? customer = await _customerRepository.GetByIdAsync(patchDto.CustomerId.Value, cancellationToken).ConfigureAwait(false);
            if (customer == null)
            {
                Logger.LogWarning("Address patch failed: Customer not found. CustomerId: {CustomerId}", patchDto.CustomerId.Value);
                throw new ArgumentException($"Customer with ID {patchDto.CustomerId.Value} not found.", nameof(patchDto));
            }
        }

        return await base.PatchAsync(id, patchDto, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    protected override bool ApplyPatch(Address entity, UpdateAddressDto patchDto)
    {
        bool hasChanges = false;
        if (patchDto.CustomerId.HasValue && entity.CustomerId != patchDto.CustomerId.Value)
        {
            entity.CustomerId = patchDto.CustomerId.Value;
            hasChanges = true;
        }
        return hasChanges
            | PartialUpdateHelper.ApplyIfChanged(entity.FirstName, patchDto.FirstName, v => entity.FirstName = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.LastName, patchDto.LastName, v => entity.LastName = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Address1, patchDto.Address1, v => entity.Address1 = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Address2, patchDto.Address2, v => entity.Address2 = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.City, patchDto.City, v => entity.City = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Zip, patchDto.Zip, v => entity.Zip = v);
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyList<AddressDto>> CreateBatchAsync(
        IReadOnlyList<CreateAddressDto> createDtos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos);

        if (createDtos.Count == 0)
        {
            return Array.Empty<AddressDto>();
        }

        Logger.LogInformation("Creating {Count} addresses in batch", createDtos.Count);

        IReadOnlyList<int> customerIds = createDtos.Select(d => d.CustomerId).Distinct().ToList();
        IReadOnlyList<Customer> customers = await _customerRepository.FindByIdsAsync(customerIds, cancellationToken).ConfigureAwait(false)
            ?? Array.Empty<Customer>();
        var customerLookup = customers.ToDictionary(c => c.Id);

        foreach (CreateAddressDto dto in createDtos)
        {
            if (!customerLookup.ContainsKey(dto.CustomerId))
            {
                Logger.LogWarning("Address creation failed: Customer not found. CustomerId: {CustomerId}", dto.CustomerId);
                throw new ArgumentException($"Customer with ID {dto.CustomerId} not found.", nameof(createDtos));
            }
        }

        return await base.CreateBatchAsync(createDtos, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override async Task<IReadOnlyList<AddressDto>> UpdateBatchAsync(
        IReadOnlyList<(int Id, UpdateAddressDto UpdateDto)> updates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        if (updates.Count == 0)
        {
            return Array.Empty<AddressDto>();
        }

        IReadOnlyList<Address> addresses = await Repository.FindByIdsAsync(updates.Select(u => u.Id).ToList(), cancellationToken).ConfigureAwait(false)
            ?? Array.Empty<Address>();
        var addressLookup = addresses.ToDictionary(a => a.Id);

        IReadOnlyList<int> customerIds = updates
            .Where(u => u.UpdateDto.CustomerId.HasValue)
            .Select(u => u.UpdateDto.CustomerId!.Value)
            .Distinct()
            .ToList();

        var customerLookup = new Dictionary<int, Customer>();
        if (customerIds.Count > 0)
        {
            IReadOnlyList<Customer> customers = await _customerRepository.FindByIdsAsync(customerIds, cancellationToken).ConfigureAwait(false)
                ?? Array.Empty<Customer>();
            customerLookup = customers.ToDictionary(c => c.Id);
        }

        List<(int Id, UpdateAddressDto UpdateDto)> validUpdates = new(updates.Count);
        foreach ((int id, UpdateAddressDto updateDto) in updates)
        {
            if (!addressLookup.TryGetValue(id, out Address? address))
            {
                continue;
            }
            if (updateDto.CustomerId.HasValue && address.CustomerId != updateDto.CustomerId.Value && !customerLookup.ContainsKey(updateDto.CustomerId.Value))
            {
                Logger.LogWarning("Address batch update skipped: Customer not found. AddressId: {AddressId}, CustomerId: {CustomerId}", id, updateDto.CustomerId.Value);
                continue;
            }
            validUpdates.Add((id, updateDto));
        }

        return await base.UpdateBatchAsync(validUpdates, cancellationToken).ConfigureAwait(false);
    }
}
