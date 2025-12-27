namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for product information.
/// </summary>
public class ProductDto
{
    /// <summary>
    /// Unique identifier for the product.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Name of the product.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Identifier of the label/brand associated with this product.
    /// </summary>
    public int? LabelId { get; set; }

    /// <summary>
    /// Product category.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gender classification.
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// Indicates whether the product is currently active.
    /// </summary>
    public bool? CurrentlyActive { get; set; }

    /// <summary>
    /// Indicates if the product record is active.
    /// </summary>
    public bool IsActive { get; set; }
}

