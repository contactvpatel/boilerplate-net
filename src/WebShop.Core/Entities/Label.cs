namespace WebShop.Core.Entities;

/// <summary>
/// Represents a brand or label.
/// </summary>
public class Label : BaseEntity
{
    /// <summary>
    /// Name of the label/brand.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// URL-friendly slug name for the label.
    /// </summary>
    public string? SlugName { get; set; }

    /// <summary>
    /// Icon image data for the label.
    /// </summary>
    public byte[]? Icon { get; set; }
}

