using System.Linq.Expressions;
using WebShop.Core.Entities;

namespace WebShop.Core.Interfaces.Base;

/// <summary>
/// Base repository interface for data access operations following Clean Architecture principles.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all entities of type T from the data store.
    /// Returns an immutable collection to prevent modification of the returned data.
    /// </summary>
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams all entities of type T from the data store as an async enumerable.
    /// Use this method for large datasets to avoid loading everything into memory at once.
    /// </summary>
    IAsyncEnumerable<T> GetAllStreamAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds entities matching the specified predicate condition.
    /// Returns an immutable collection to prevent modification of the returned data.
    /// </summary>
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an entity to the repository. Changes are NOT saved automatically.
    /// You MUST call SaveChangesAsync() after this method to persist changes.
    /// </summary>
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an entity in the repository. Changes are NOT saved automatically.
    /// You MUST call SaveChangesAsync() after this method to persist changes.
    /// </summary>
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Soft deletes an entity in the repository (sets IsActive = false). Changes are NOT saved automatically.
    /// You MUST call SaveChangesAsync() after this method to persist changes.
    /// </summary>
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if an entity with the specified ID exists.
    /// </summary>
    /// <param name="id">The entity ID to check.</param>
    /// <param name="includeSoftDeleted">If true, includes soft-deleted entities in the check.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the entity exists, false otherwise.</returns>
    Task<bool> ExistsAsync(int id, bool includeSoftDeleted = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves all changes made in the write context to the database.
    /// This method MUST be called after AddAsync, UpdateAsync, or DeleteAsync operations to persist changes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of entities.
    /// </summary>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the items for the page and the total count of all items.</returns>
    Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a paginated list of entities matching the specified predicate.
    /// </summary>
    /// <param name="predicate">The filter predicate.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A tuple containing the filtered items for the page and the total count of filtered items.</returns>
    Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>> predicate, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
}
