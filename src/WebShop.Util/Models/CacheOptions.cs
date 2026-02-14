using System.ComponentModel.DataAnnotations;

namespace WebShop.Util.Models;

/// <summary>
/// Configuration options for HybridCache.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Whether caching is enabled.
    /// Default: true
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Default expiration time for cache entries as a string (e.g., "00:10:00" for 10 minutes).
    /// If not specified, defaults to 10 minutes.
    /// </summary>
    [RegularExpression(@"^(\d+\.)?[0-9]{1,2}:[0-9]{2}:[0-9]{2}(\.[0-9]+)?$", ErrorMessage = "DefaultExpiration must be in TimeSpan format (e.g., 00:10:00 for 10 minutes, 1.00:00:00 for 1 day).")]
    public string? DefaultExpiration { get; set; }

    /// <summary>
    /// Default local (in-memory) cache expiration time as a string (e.g., "00:05:00" for 5 minutes).
    /// This is typically shorter than DefaultExpiration for faster invalidation.
    /// If not specified, defaults to 5 minutes.
    /// </summary>
    [RegularExpression(@"^(\d+\.)?[0-9]{1,2}:[0-9]{2}:[0-9]{2}(\.[0-9]+)?$", ErrorMessage = "DefaultLocalExpiration must be in TimeSpan format (e.g., 00:05:00 for 5 minutes, 1.00:00:00 for 1 day).")]
    public string? DefaultLocalExpiration { get; set; }

    /// <summary>
    /// Gets the default expiration as a TimeSpan, or null if not set.
    /// </summary>
    /// <returns>The default expiration as a TimeSpan, or null if not set or invalid format.</returns>
    /// <exception cref="InvalidOperationException">Thrown when DefaultExpiration is set but has an invalid format.</exception>
    public TimeSpan? GetDefaultExpiration()
    {
        return ParseExpiration(DefaultExpiration, nameof(DefaultExpiration));
    }

    /// <summary>
    /// Gets the default local expiration as a TimeSpan, or null if not set.
    /// </summary>
    /// <returns>The default local expiration as a TimeSpan, or null if not set or invalid format.</returns>
    /// <exception cref="InvalidOperationException">Thrown when DefaultLocalExpiration is set but has an invalid format.</exception>
    public TimeSpan? GetDefaultLocalExpiration()
    {
        return ParseExpiration(DefaultLocalExpiration, nameof(DefaultLocalExpiration));
    }

    private static TimeSpan? ParseExpiration(string? value, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!TimeSpan.TryParse(value, out TimeSpan result))
        {
            throw new InvalidOperationException(
                $"Invalid {propertyName} format: '{value}'. Expected format: 'hh:mm:ss' or 'd.hh:mm:ss' (e.g., '00:10:00' for 10 minutes, '1.00:00:00' for 1 day).");
        }

        return result;
    }

    /// <summary>
    /// Maximum size of a cache entry in bytes.
    /// Entries larger than this will not be cached.
    /// Default is 1 MB (1,048,576 bytes).
    /// </summary>
    [Range(1024, long.MaxValue, ErrorMessage = "MaximumPayloadBytes must be at least 1 KB when specified.")]
    public long? MaximumPayloadBytes { get; set; }

    /// <summary>
    /// Maximum length of a cache key in characters.
    /// Keys longer than this will bypass the cache.
    /// Default is 1024 characters.
    /// </summary>
    [Range(1, 4096, ErrorMessage = "MaximumKeyLength must be between 1 and 4096 when specified.")]
    public int? MaximumKeyLength { get; set; }

    /// <summary>
    /// Connection string for redis distributed cache.
    /// If not specified, only in-memory caching will be used.
    /// </summary>
    [MinLength(1, ErrorMessage = "RedisConnectionString cannot be empty when specified.")]
    public string? RedisConnectionString { get; set; }

    /// <summary>
    /// Redis instance name for key prefixing (e.g., "WebShop:").
    /// Useful when multiple applications share the same Redis instance.
    /// If not specified, no prefix is used.
    /// </summary>
    [MaxLength(256, ErrorMessage = "RedisInstanceName cannot exceed 256 characters.")]
    public string? RedisInstanceName { get; set; }
}

