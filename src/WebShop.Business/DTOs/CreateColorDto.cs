using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for creating a new color.
/// </summary>
public class CreateColorDto
{
    /// <summary>
    /// Name of the color.
    /// </summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// RGB hex value of the color (e.g., #FF0000).
    /// </summary>
    [StringLength(7)]
    [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "RGB must be in hex format (e.g., #FF0000)")]
    public string? Rgb { get; set; }
}
