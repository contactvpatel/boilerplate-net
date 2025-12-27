namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for label/brand information.
/// </summary>
public class LabelDto
{
    /// <summary>
    /// Unique identifier for the label.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the label/brand.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// URL-friendly slug name for the label.
    /// </summary>
    public string? SlugName { get; set; }

    /// <summary>
    /// Indicates if the label record is active.
    /// </summary>
    public bool IsActive { get; set; }
}

