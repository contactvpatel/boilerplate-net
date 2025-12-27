using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Request DTO for clearing cache by multiple keys.
/// </summary>
public class ClearCacheByKeysRequest
{
    /// <summary>
    /// List of cache keys to remove.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> Keys { get; set; } = new();
}

