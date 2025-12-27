using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for creating a new order.
/// </summary>
public class CreateOrderDto
{
    /// <summary>
    /// Identifier of the customer who placed the order.
    /// </summary>
    [Required]
    public int CustomerId { get; set; }

    /// <summary>
    /// Identifier of the shipping address for this order.
    /// </summary>
    [Required]
    public int ShippingAddressId { get; set; }

    /// <summary>
    /// Shipping cost for the order.
    /// </summary>
    [Range(0, double.MaxValue)]
    public decimal ShippingCost { get; set; }
}

