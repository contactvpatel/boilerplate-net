namespace WebShop.Core.Entities;

/// <summary>
/// Represents the amount of articles available in stock.
/// </summary>
public class Stock : BaseEntity
{
    /// <summary>
    /// Identifier of the article this stock entry refers to.
    /// </summary>
    public int? ArticleId { get; set; }

    /// <summary>
    /// Number of items available in stock.
    /// </summary>
    public int? Count { get; set; }
}

