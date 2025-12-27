namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for order position information.
/// </summary>
public class OrderPositionDto
{
    /// <summary>
    /// Unique identifier for the order position.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identifier of the order this position belongs to.
    /// </summary>
    public int? OrderId { get; set; }

    /// <summary>
    /// Identifier of the article in this order position.
    /// </summary>
    public int? ArticleId { get; set; }

    /// <summary>
    /// Quantity of articles ordered.
    /// </summary>
    public short? Amount { get; set; }

    /// <summary>
    /// Price per unit at the time of order.
    /// </summary>
    public decimal? Price { get; set; }

    /// <summary>
    /// Indicates if the order position record is active.
    /// </summary>
    public bool IsActive { get; set; }
}

