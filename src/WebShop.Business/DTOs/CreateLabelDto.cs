using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for creating a new label/brand.
/// </summary>
public class CreateLabelDto
{
    /// <summary>
    /// Name of the label/brand.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// URL-friendly slug name for the label.
    /// </summary>
    [StringLength(200)]
    public string? SlugName { get; set; }
}
