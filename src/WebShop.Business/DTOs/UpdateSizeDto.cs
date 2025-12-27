using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for updating an existing size.
/// </summary>
public class UpdateSizeDto
{
    /// <summary>
    /// Gender classification (male, female, unisex).
    /// </summary>
    [StringLength(20)]
    public string? Gender { get; set; }

    /// <summary>
    /// Product category this size applies to.
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Size label (e.g., XS, S, M, L, XL).
    /// </summary>
    [StringLength(20)]
    public string? SizeLabel { get; set; }

    /// <summary>
    /// US size range.
    /// </summary>
    [StringLength(50)]
    public string? SizeUs { get; set; }

    /// <summary>
    /// UK size range.
    /// </summary>
    [StringLength(50)]
    public string? SizeUk { get; set; }

    /// <summary>
    /// EU size range.
    /// </summary>
    [StringLength(50)]
    public string? SizeEu { get; set; }
}
