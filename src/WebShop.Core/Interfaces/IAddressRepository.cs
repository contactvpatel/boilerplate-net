using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Core.Interfaces;

/// <summary>
/// Repository interface for address data access operations.
/// </summary>
public interface IAddressRepository : IRepository<Address>
{
    /// <summary>
    /// Gets all addresses associated with a specific customer.
    /// </summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of addresses for the customer.</returns>
    Task<List<Address>> GetByCustomerIdAsync(int customerId, CancellationToken cancellationToken = default);
}

