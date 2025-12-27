using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for updating an existing label/brand.
/// </summary>
public class UpdateLabelDto
{
    /// <summary>
    /// Name of the label/brand.
    /// </summary>
    [StringLength(200)]
    public string? Name { get; set; }

    /// <summary>
    /// URL-friendly slug name for the label.
    /// </summary>
    [StringLength(200)]
    public string? SlugName { get; set; }
}
