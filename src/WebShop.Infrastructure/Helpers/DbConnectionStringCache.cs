using System.Collections.Concurrent;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Thread-safe cache for database connection strings to avoid recreating them on each context creation.
/// This improves performance by eliminating repeated string concatenation and parsing operations.
/// </summary>
internal static class DbConnectionStringCache
{
    private static readonly ConcurrentDictionary<string, string> _cache = new();

    /// <summary>
    /// Gets or creates a cached connection string for the given connection model.
    /// </summary>
    /// <param name="connectionModel">The connection model to create a connection string for.</param>
    /// <param name="factory">Factory function to create the connection string if not cached.</param>
    /// <returns>The cached or newly created connection string.</returns>
    public static string GetOrCreate(ConnectionModel connectionModel, Func<string> factory)
    {
        // Create a cache key from connection model properties
        // This ensures we cache per unique connection configuration
        string cacheKey = $"{connectionModel.Host}:{connectionModel.Port}:{connectionModel.DatabaseName}:{connectionModel.UserId}";

        return _cache.GetOrAdd(cacheKey, _ => factory());
    }

    /// <summary>
    /// Clears the connection string cache. Useful for testing or when connection settings change.
    /// </summary>
    public static void Clear()
    {
        _cache.Clear();
    }
}
