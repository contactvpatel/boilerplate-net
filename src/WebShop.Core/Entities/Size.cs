namespace WebShop.Core.Entities;

/// <summary>
/// Represents a size definition with conversions for US, UK, and EU.
/// </summary>
public class Size : BaseEntity
{
    /// <summary>
    /// Gender classification (male, female, unisex).
    /// </summary>
    public string? Gender { get; set; }

    /// <summary>
    /// Product category this size applies to.
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Size label (e.g., XS, S, M, L, XL).
    /// </summary>
    public string? SizeLabel { get; set; }

    /// <summary>
    /// US size range.
    /// </summary>
    public string? SizeUs { get; set; }

    /// <summary>
    /// UK size range.
    /// </summary>
    public string? SizeUk { get; set; }

    /// <summary>
    /// EU size range.
    /// </summary>
    public string? SizeEu { get; set; }
}

