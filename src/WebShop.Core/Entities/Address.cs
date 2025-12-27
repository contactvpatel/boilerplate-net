namespace WebShop.Core.Entities;

/// <summary>
/// Represents an address for receipts and shipping.
/// </summary>
public class Address : BaseEntity
{
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
    /// Secondary address line (apartment, suite, etc.).
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
}

