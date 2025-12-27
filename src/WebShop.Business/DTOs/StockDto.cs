namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for stock information.
/// </summary>
public class StockDto
{
    /// <summary>
    /// Unique identifier for the stock entry.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identifier of the article this stock entry refers to.
    /// </summary>
    public int? ArticleId { get; set; }

    /// <summary>
    /// Number of items available in stock.
    /// </summary>
    public int? Count { get; set; }

    /// <summary>
    /// Indicates if the stock record is active.
    /// </summary>
    public bool IsActive { get; set; }
}

