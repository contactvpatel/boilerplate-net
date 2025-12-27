using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for updating an existing address.
/// </summary>
public class UpdateAddressDto
{
    /// <summary>
    /// Identifier of the customer this address belongs to.
    /// </summary>
    public int? CustomerId { get; set; }

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
    [StringLength(500)]
    public string? Address1 { get; set; }

    /// <summary>
    /// Secondary address line.
    /// </summary>
    [StringLength(500)]
    public string? Address2 { get; set; }

    /// <summary>
    /// City name.
    /// </summary>
    [StringLength(200)]
    public string? City { get; set; }

    /// <summary>
    /// ZIP or postal code.
    /// </summary>
    [StringLength(20)]
    public string? Zip { get; set; }
}

