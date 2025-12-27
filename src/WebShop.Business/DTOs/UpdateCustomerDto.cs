using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for updating an existing customer.
/// </summary>
public class UpdateCustomerDto
{
    /// <summary>
    /// First name of the customer.
    /// </summary>
    [StringLength(200)]
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name of the customer.
    /// </summary>
    [StringLength(200)]
    public string? LastName { get; set; }

    /// <summary>
    /// Gender of the customer (male, female, unisex).
    /// </summary>
    [StringLength(20)]
    public string? Gender { get; set; }

    /// <summary>
    /// Email address of the customer.
    /// </summary>
    [EmailAddress]
    [StringLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// Date of birth of the customer.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }

    /// <summary>
    /// Identifier of the customer's current address.
    /// </summary>
    public int? CurrentAddressId { get; set; }
}

