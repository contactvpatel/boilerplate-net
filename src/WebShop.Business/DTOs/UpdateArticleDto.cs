using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for updating an existing article.
/// </summary>
public class UpdateArticleDto
{
    /// <summary>
    /// Identifier of the product this article belongs to.
    /// </summary>
    public int? ProductId { get; set; }

    /// <summary>
    /// European Article Number (EAN) barcode.
    /// </summary>
    [StringLength(50)]
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
    [StringLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Original price of the article.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? OriginalPrice { get; set; }

    /// <summary>
    /// Reduced/sale price of the article.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? ReducedPrice { get; set; }

    /// <summary>
    /// Tax rate applied to this article.
    /// </summary>
    [Range(0, 100)]
    public decimal? TaxRate { get; set; }

    /// <summary>
    /// Discount percentage applied to this article.
    /// </summary>
    [Range(0, 100)]
    public int? DiscountInPercent { get; set; }

    /// <summary>
    /// Indicates whether the article is currently active.
    /// </summary>
    public bool? CurrentlyActive { get; set; }
}
