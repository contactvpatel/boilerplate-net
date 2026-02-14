using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Core.Interfaces;

/// <summary>
/// Repository interface for customer data access operations.
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    /// <summary>
    /// Gets a customer by email address.
    /// </summary>
    /// <param name="email">Email address to search for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Customer if found, otherwise null.</returns>
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all customers as a list. Does not include address data; use separate address queries if needed.
    /// Prefer <see cref="IRepository{Customer}.GetAllAsync"/> for standard read-only access.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of customers.</returns>
    Task<List<Customer>> GetAllAsListAsync(CancellationToken cancellationToken = default);
}

