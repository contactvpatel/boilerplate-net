namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for order information.
/// </summary>
public class OrderDto
{
    /// <summary>
    /// Unique identifier for the order.
    /// </summary>
    public int Id { get; set; }

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

    /// <summary>
    /// Indicates if the order record is active.
    /// </summary>
    public bool IsActive { get; set; }
}

