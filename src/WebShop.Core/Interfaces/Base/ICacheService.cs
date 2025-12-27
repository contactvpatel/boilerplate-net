namespace WebShop.Core.Interfaces.Base;

/// <summary>
/// Cache service interface for storing and retrieving cached data.
/// Provides a clean abstraction over the underlying cache implementation (HybridCache).
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets a value from the cache, or creates it if it doesn't exist.
    /// This method provides stampede protection - only one concurrent caller will execute the factory.
    /// </summary>
    /// <typeparam name="T">The type of the cached value.</typeparam>
    /// <param name="key">The cache key. Must be unique and not contain user input directly.</param>
    /// <param name="factory">The factory method to create the value if it's not in cache.</param>
    /// <param name="expiration">Optional expiration time. If null, uses default cache expiration.</param>
    /// <param name="localExpiration">Optional local (in-memory) cache expiration. If null, uses default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached or newly created value.</returns>
    Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        CancellationToken cancellationToken = default) where T : notnull;

    /// <summary>
    /// Sets a value in the cache with optional expiration.
    /// </summary>
    /// <typeparam name="T">The type of the value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    /// <param name="expiration">Optional expiration time. If null, uses default cache expiration.</param>
    /// <param name="localExpiration">Optional local (in-memory) cache expiration. If null, uses default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        CancellationToken cancellationToken = default) where T : notnull;

    /// <summary>
    /// Removes a value from the cache by key.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes multiple values from the cache by keys.
    /// </summary>
    /// <param name="keys">The cache keys to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries associated with a tag.
    /// </summary>
    /// <param name="tag">The tag to remove entries for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes all cache entries associated with multiple tags.
    /// </summary>
    /// <param name="tags">The tags to remove entries for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveByTagAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
}
