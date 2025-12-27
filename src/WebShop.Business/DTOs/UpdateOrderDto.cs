using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for updating an existing order.
/// </summary>
public class UpdateOrderDto
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
    [Range(0, double.MaxValue)]
    public decimal? Total { get; set; }

    /// <summary>
    /// Shipping cost for the order.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal? ShippingCost { get; set; }
}
