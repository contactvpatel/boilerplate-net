using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Core.Interfaces;

/// <summary>
/// Repository interface for order data access operations.
/// </summary>
public interface IOrderRepository : IRepository<Order>
{
    /// <summary>
    /// Gets all orders placed by a specific customer.
    /// </summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of orders for the customer.</returns>
    Task<List<Order>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets orders created within the specified date range (inclusive).
    /// </summary>
    /// <param name="startDate">Start date of the range (inclusive).</param>
    /// <param name="endDate">End date of the range (inclusive).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of orders within the date range.</returns>
    Task<List<Order>> GetByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
}

