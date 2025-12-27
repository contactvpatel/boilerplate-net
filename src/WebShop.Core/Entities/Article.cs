namespace WebShop.Core.Entities;

/// <summary>
/// Represents an instance of a product with a specific size, color, and price.
/// </summary>
public class Article : BaseEntity
{
    /// <summary>
    /// Identifier of the product this article belongs to.
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// European Article Number (EAN) barcode.
    /// </summary>
    public string? Ean { get; set; }

    /// <summary>
    /// Identifier of the color for this article.
    /// </summary>
    public int? ColorId { get; set; }

    /// <summary>
    /// Size identifier for this article.
    /// </summary>
    public int? Size { get; set; }

    /// <summary>
    /// Description of the article.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Original price of the article.
    /// </summary>
    public decimal? OriginalPrice { get; set; }

    /// <summary>
    /// Reduced/sale price of the article.
    /// </summary>
    public decimal? ReducedPrice { get; set; }

    /// <summary>
    /// Tax rate applied to this article.
    /// </summary>
    public decimal? TaxRate { get; set; }

    /// <summary>
    /// Discount percentage applied to this article.
    /// </summary>
    public int? DiscountInPercent { get; set; }

    /// <summary>
    /// Indicates whether the article is currently active.
    /// </summary>
    public bool? CurrentlyActive { get; set; }
}

