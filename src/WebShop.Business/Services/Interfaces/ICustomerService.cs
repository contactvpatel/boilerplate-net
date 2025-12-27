using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Service interface for customer business operations.
/// </summary>
public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all customers in the system.
    /// </summary>
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of customers.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the paginated items and total count.</returns>
    Task<(IReadOnlyList<CustomerDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<CustomerDto> CreateAsync(CreateCustomerDto createDto, CancellationToken cancellationToken = default);
    Task<CustomerDto?> UpdateAsync(int id, UpdateCustomerDto updateDto, CancellationToken cancellationToken = default);
    Task<CustomerDto?> PatchAsync(int id, UpdateCustomerDto patchDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates multiple customers in a batch operation.
    /// </summary>
    Task<IReadOnlyList<CustomerDto>> CreateBatchAsync(IReadOnlyList<CreateCustomerDto> createDtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple customers in a batch operation.
    /// </summary>
    Task<IReadOnlyList<CustomerDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateCustomerDto UpdateDto)> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple customers in a batch operation (soft delete).
    /// </summary>
    Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);
    Task<CustomerDto?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
}

