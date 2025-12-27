namespace WebShop.Core.Entities;

/// <summary>
/// Represents an order with metadata. See OrderPosition for order line items.
/// </summary>
public class Order : BaseEntity
{
    /// <summary>
    /// Identifier of the customer who placed the order.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Timestamp when the order was placed.
    /// </summary>
    public DateTime? OrderTimestamp { get; set; }

    /// <summary>
    /// Identifier of the shipping address for this order.
    /// </summary>
    public int? ShippingAddressId { get; set; }

    /// <summary>
    /// Total amount of the order.
    /// </summary>
    public decimal? Total { get; set; }

    /// <summary>
    /// Shipping cost for the order.
    /// </summary>
    public decimal? ShippingCost { get; set; }
}

