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
/// Service implementation for customer business operations.
/// </summary>
public class CustomerService(
    ICustomerRepository customerRepository,
    IUnitOfWork unitOfWork,
    ILogger<CustomerService> logger)
    : CrudServiceBase<Customer, CustomerDto, CreateCustomerDto, UpdateCustomerDto>(customerRepository, unitOfWork, logger),
        Interfaces.ICustomerService
{
    /// <inheritdoc />
    protected override string EntityName => "Customer";

    /// <inheritdoc />
    protected override string EntityNamePlural => "Customers";

    /// <inheritdoc />
    public new async Task<Result<CustomerDto>> CreateAsync(CreateCustomerDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        Customer? existing = await customerRepository.GetByEmailAsync(createDto.Email, cancellationToken).ConfigureAwait(false);
        if (existing != null)
        {
            return Result<CustomerDto>.Failure("Email address is already in use. Please use a different email address.");
        }

        CustomerDto created = await CreateCoreAsync(createDto, cancellationToken).ConfigureAwait(false);
        return Result<CustomerDto>.Success(created);
    }

    /// <inheritdoc />
    public async Task<Result<CustomerDto>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        Customer? customer = await customerRepository.GetByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        return customer == null ? Result<CustomerDto>.NotFound() : Result<CustomerDto>.Success(customer.Adapt<CustomerDto>());
    }

    /// <inheritdoc />
    protected override bool ApplyPatch(Customer entity, UpdateCustomerDto patchDto)
    {
        return PartialUpdateHelper.ApplyIfChanged(entity.FirstName, patchDto.FirstName, v => entity.FirstName = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.LastName, patchDto.LastName, v => entity.LastName = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Email, patchDto.Email, v => entity.Email = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Gender, patchDto.Gender, v => entity.Gender = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.DateOfBirth, patchDto.DateOfBirth, v => entity.DateOfBirth = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.CurrentAddressId, patchDto.CurrentAddressId, v => entity.CurrentAddressId = v);
    }
}
