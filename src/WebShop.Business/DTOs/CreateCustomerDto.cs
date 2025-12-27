using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for creating a new customer.
/// </summary>
public class CreateCustomerDto
{
    /// <summary>
    /// First name of the customer.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name of the customer.
    /// </summary>
    [Required]
    [StringLength(200)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Gender of the customer (male, female, unisex).
    /// </summary>
    [StringLength(20)]
    public string? Gender { get; set; }

    /// <summary>
    /// Email address of the customer.
    /// </summary>
    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Date of birth of the customer.
    /// </summary>
    public DateTime? DateOfBirth { get; set; }
}

