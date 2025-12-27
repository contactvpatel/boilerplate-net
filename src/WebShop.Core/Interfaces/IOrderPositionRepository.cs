using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Core.Interfaces;

/// <summary>
/// Repository interface for order position data access operations.
/// </summary>
public interface IOrderPositionRepository : IRepository<OrderPosition>
{
    /// <summary>
    /// Gets all order line items (positions) for a specific order.
    /// </summary>
    /// <param name="orderId">Order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of order positions (line items) for the specified order.</returns>
    Task<List<OrderPosition>> GetByOrderIdAsync(int orderId, CancellationToken cancellationToken = default);
}

