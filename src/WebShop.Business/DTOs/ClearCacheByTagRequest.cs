using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Request DTO for clearing cache by tag.
/// </summary>
public class ClearCacheByTagRequest
{
    /// <summary>
    /// The tag to remove all associated cache entries for.
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Tag { get; set; } = string.Empty;
}

