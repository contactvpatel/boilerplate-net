namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for color information.
/// </summary>
public class ColorDto
{
    /// <summary>
    /// Unique identifier for the color.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the color.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// RGB hex value of the color.
    /// </summary>
    public string? Rgb { get; set; }

    /// <summary>
    /// Indicates if the color record is active.
    /// </summary>
    public bool IsActive { get; set; }
}

