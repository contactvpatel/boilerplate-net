namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for address information.
/// </summary>
public class AddressDto
{
    /// <summary>
    /// Unique identifier for the address.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identifier of the customer this address belongs to.
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// First name for the address.
    /// </summary>
    public string? FirstName { get; set; }

    /// <summary>
    /// Last name for the address.
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// Primary address line.
    /// </summary>
    public string? Address1 { get; set; }

    /// <summary>
    /// Secondary address line.
    /// </summary>
    public string? Address2 { get; set; }

    /// <summary>
    /// City name.
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// ZIP or postal code.
    /// </summary>
    public string? Zip { get; set; }

    /// <summary>
    /// Indicates if the address record is active.
    /// </summary>
    public bool IsActive { get; set; }
}

