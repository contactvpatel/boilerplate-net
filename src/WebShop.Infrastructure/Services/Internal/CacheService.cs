using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebShop.Core.Interfaces.Base;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.Services.Internal;

/// <summary>
/// Implementation of ICacheService using HybridCache.
/// HybridCache provides both in-memory (primary) and distributed (secondary) caching
/// with automatic stampede protection and optimal performance.
/// When caching is disabled, factory methods are executed directly (bypassing cache).
/// </summary>
public class CacheService(
    HybridCache? cache,
    IOptions<CacheOptions> cacheOptions,
    ILogger<CacheService> logger) : ICacheService
{
    private readonly HybridCache? _cache = cache;
    private readonly CacheOptions _cacheOptions = cacheOptions?.Value ?? new CacheOptions();
    private readonly ILogger<CacheService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        CancellationToken cancellationToken = default) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
        }

        // If caching is disabled or HybridCache is not available, execute factory directly
        if (!_cacheOptions.Enabled || _cache == null)
        {
            _logger.LogDebug("Cache disabled - executing factory directly for key: {Key}", key);
            return await factory(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            HybridCacheEntryOptions? options = null;
            if (expiration.HasValue || localExpiration.HasValue)
            {
                options = new HybridCacheEntryOptions
                {
                    Expiration = expiration ?? TimeSpan.FromMinutes(10),
                    LocalCacheExpiration = localExpiration ?? expiration ?? TimeSpan.FromMinutes(5)
                };
            }

            // Convert Task<T> factory to ValueTask<T> for HybridCache
            async ValueTask<T> factoryWrapper(CancellationToken cancel) => await factory(cancel).ConfigureAwait(false);

            T result = await _cache.GetOrCreateAsync(
                key,
                factoryWrapper,
                options,
                tags: null,
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Cache {Action} for key: {Key}", "hit or created", key);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting or creating cache entry for key: {Key}", key);
            // If cache fails, execute factory directly to ensure functionality
            return await factory(cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiration = null,
        TimeSpan? localExpiration = null,
        CancellationToken cancellationToken = default) where T : notnull
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be null or empty.", nameof(key));
        }

        // If caching is disabled or HybridCache is not available, skip set operation
        if (!_cacheOptions.Enabled || _cache == null)
        {
            _logger.LogDebug("Cache disabled - skipping set operation for key: {Key}", key);
            return;
        }

        try
        {
            HybridCacheEntryOptions? options = null;
            if (expiration.HasValue || localExpiration.HasValue)
            {
                options = new HybridCacheEntryOptions
                {
                    Expiration = expiration ?? TimeSpan.FromMinutes(10),
                    LocalCacheExpiration = localExpiration ?? expiration ?? TimeSpan.FromMinutes(5)
                };
            }

            await _cache.SetAsync(key, value, options, tags: null, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Cache entry set for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cache entry for key: {Key}", key);
            // Swallow exception to prevent cache failures from breaking application flow
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        // If caching is disabled or HybridCache is not available, skip remove operation
        if (!_cacheOptions.Enabled || _cache == null)
        {
            _logger.LogDebug("Cache disabled - skipping remove operation for key: {Key}", key);
            return;
        }

        try
        {
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Cache entry removed for key: {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entry for key: {Key}", key);
        }
    }

    /// <inheritdoc />
    public async Task RemoveAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        if (keys == null)
        {
            return;
        }

        List<Task> removeTasks = keys.Select(key => RemoveAsync(key, cancellationToken)).ToList();
        await Task.WhenAll(removeTasks).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return;
        }

        // If caching is disabled or HybridCache is not available, skip remove operation
        if (!_cacheOptions.Enabled || _cache == null)
        {
            _logger.LogDebug("Cache disabled - skipping remove by tag operation for tag: {Tag}", tag);
            return;
        }

        try
        {
            await _cache.RemoveByTagAsync(tag, cancellationToken).ConfigureAwait(false);
            _logger.LogDebug("Cache entries removed for tag: {Tag}", tag);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cache entries for tag: {Tag}", tag);
        }
    }

    /// <inheritdoc />
    public async Task RemoveByTagAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        if (tags == null)
        {
            return;
        }

        List<Task> removeTasks = tags.Select(tag => RemoveByTagAsync(tag, cancellationToken)).ToList();
        await Task.WhenAll(removeTasks).ConfigureAwait(false);
    }
}
