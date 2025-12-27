using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Service interface for address business operations.
/// </summary>
public interface IAddressService
{
    Task<AddressDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all addresses in the system.
    /// </summary>
    Task<IReadOnlyList<AddressDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<AddressDto> CreateAsync(CreateAddressDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all addresses associated with a specific customer.
    /// </summary>
    Task<IReadOnlyList<AddressDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an address by setting IsActive to false.
    /// </summary>
    /// <param name="id">The address identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the address was found and deleted, false otherwise.</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing address (full update - PUT operation).
    /// This operation is idempotent: sending the same request multiple times produces the same result.
    /// </summary>
    /// <param name="id">The address identifier.</param>
    /// <param name="updateDto">The address update data containing all fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated address DTO if found and updated, null otherwise.</returns>
    Task<AddressDto?> UpdateAsync(int id, UpdateAddressDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Partially updates an existing address (partial update - PATCH operation).
    /// This operation is idempotent: sending the same request multiple times produces the same result.
    /// Only provided fields are updated; null values are ignored.
    /// </summary>
    /// <param name="id">The address identifier.</param>
    /// <param name="patchDto">The address patch data containing only fields to update.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated address DTO if found, null otherwise. Returns address even if no changes were made (idempotent).</returns>
    Task<AddressDto?> PatchAsync(int id, UpdateAddressDto patchDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple addresses in a batch operation.
    /// </summary>
    Task<IReadOnlyList<AddressDto>> CreateBatchAsync(IReadOnlyList<CreateAddressDto> createDtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple addresses in a batch operation.
    /// </summary>
    Task<IReadOnlyList<AddressDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateAddressDto UpdateDto)> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple addresses in a batch operation (soft delete).
    /// </summary>
    Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);
}

