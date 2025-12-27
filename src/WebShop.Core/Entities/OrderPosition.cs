namespace WebShop.Core.Entities;

/// <summary>
/// Represents an article that is contained in an order.
/// </summary>
public class OrderPosition : BaseEntity
{
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
}

