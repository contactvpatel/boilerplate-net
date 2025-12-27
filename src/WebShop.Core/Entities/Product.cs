namespace WebShop.Core.Entities;

/// <summary>
/// Represents a product group that contains multiple articles (differing in sizes/color).
/// </summary>
public class Product : BaseEntity
{
    /// <summary>
    /// Name of the product.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Identifier of the label/brand associated with this product.
    /// </summary>
    public int? LabelId { get; set; }

    /// <summary>
    /// Product category (Apparel, Footwear, Sportswear, etc.).
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Gender classification (male, female, unisex).
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// Indicates whether the product is currently active.
    /// </summary>
    public bool? CurrentlyActive { get; set; }
}

