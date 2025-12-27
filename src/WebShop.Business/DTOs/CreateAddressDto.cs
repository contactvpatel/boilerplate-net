using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for creating a new address.
/// </summary>
public class CreateAddressDto
{
    /// <summary>
    /// Identifier of the customer this address belongs to.
    /// </summary>
    [Required]
    public int CustomerId { get; set; }

    /// <summary>
    /// First name for the address.
    /// </summary>
    [StringLength(200)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name for the address.
    /// </summary>
    [StringLength(200)]
    public string? LastName { get; set; }

    /// <summary>
    /// Primary address line.
    /// </summary>
    [Required]
    [StringLength(500)]
    public string Address1 { get; set; } = string.Empty;

    /// <summary>
    /// Secondary address line.
    /// </summary>
    [StringLength(500)]
    public string? Address2 { get; set; }

    /// <summary>
    /// City name.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// ZIP or postal code.
    /// </summary>
    [Required]
    [StringLength(20)]
    public string Zip { get; set; } = string.Empty;
}

