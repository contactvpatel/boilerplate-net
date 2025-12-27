using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for updating an existing stock entry.
/// </summary>
public class UpdateStockDto
{
    /// <summary>
    /// Identifier of the article this stock entry refers to.
    /// </summary>
    public int? ArticleId { get; set; }

    /// <summary>
    /// Number of items available in stock.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int? Count { get; set; }
}
