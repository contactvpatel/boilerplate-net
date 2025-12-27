namespace WebShop.Core.Entities;

/// <summary>
/// Represents a customer in the webshop system.
/// </summary>
public class Customer : BaseEntity
{
    /// <summary>
    /// First name of the customer.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name of the customer.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gender of the customer (male, female, unisex).
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// Email address of the customer.
    /// </summary>
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

