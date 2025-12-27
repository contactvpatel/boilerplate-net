using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Request DTO for clearing cache by multiple tags.
/// </summary>
public class ClearCacheByTagsRequest
{
    /// <summary>
    /// List of tags to remove all associated cache entries for.
    /// </summary>
    [Required]
    [MinLength(1)]
    public List<string> Tags { get; set; } = new();
}

