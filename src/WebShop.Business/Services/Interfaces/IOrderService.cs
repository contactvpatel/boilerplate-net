using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Service interface for order business operations.
/// </summary>
public interface IOrderService
{
    Task<OrderDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders in the system.
    /// </summary>
    Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of orders.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the paginated items and total count.</returns>
    Task<(IReadOnlyList<OrderDto> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<OrderDto> CreateAsync(CreateOrderDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all orders placed by a specific customer.
    /// </summary>
    Task<IReadOnlyList<OrderDto>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders created within the specified date range (inclusive).
    /// </summary>
    Task<IReadOnlyList<OrderDto>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    Task<OrderDto?> UpdateAsync(int id, UpdateOrderDto updateDto, CancellationToken cancellationToken = default);
    Task<OrderDto?> PatchAsync(int id, UpdateOrderDto patchDto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates multiple orders in a batch operation.
    /// </summary>
    Task<IReadOnlyList<OrderDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateOrderDto UpdateDto)> updates, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple orders in a batch operation (soft delete).
    /// </summary>
    Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);
}

