using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for updating an existing color.
/// </summary>
public class UpdateColorDto
{
    /// <summary>
    /// Name of the color.
    /// </summary>
    [StringLength(100)]
    public string? Name { get; set; }

    /// <summary>
    /// RGB hex value of the color (e.g., #FF0000).
    /// </summary>
    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "RGB must be in hex format (e.g., #FF0000)")]
    public string? Rgb { get; set; }
}
