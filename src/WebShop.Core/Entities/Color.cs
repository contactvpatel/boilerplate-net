namespace WebShop.Core.Entities;

/// <summary>
/// Represents a color with name and RGB value.
/// </summary>
public class Color : BaseEntity
{
    /// <summary>
    /// Name of the color.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// RGB hex value of the color (e.g., #FF0000).
    /// </summary>
    public string? Rgb { get; set; }
}

