namespace WebShop.Business.DTOs;

/// <summary>
/// Result DTO for cache operations.
/// </summary>
public class CacheOperationResultDto
{
    /// <summary>
    /// Indicates whether the cache operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Number of cache entries affected by the operation.
    /// </summary>
    public int EntriesAffected { get; set; }

    /// <summary>
    /// Message describing the result of the operation.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

