namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for size information.
/// </summary>
public class SizeDto
{
    /// <summary>
    /// Unique identifier for the size.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gender classification.
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

    /// <summary>
    /// Indicates if the size record is active.
    /// </summary>
    public bool IsActive { get; set; }
}

