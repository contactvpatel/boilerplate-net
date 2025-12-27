namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for customer information.
/// </summary>
public class CustomerDto
{
    /// <summary>
    /// Unique identifier for the customer.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// First name of the customer.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name of the customer.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Gender of the customer.
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

    /// <summary>
    /// Indicates if the customer record is active.
    /// </summary>
    public bool IsActive { get; set; }
}

