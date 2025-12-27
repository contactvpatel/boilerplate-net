using System.ComponentModel.DataAnnotations;

namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object for updating an existing product.
/// </summary>
public class UpdateProductDto
{
    /// <summary>
    /// Name of the product.
    /// </summary>
    [StringLength(500)]
    public string? Name { get; set; }

    /// <summary>
    /// Identifier of the label/brand associated with this product.
    /// </summary>
    public int? LabelId { get; set; }

    /// <summary>
    /// Product category.
    /// </summary>
    [StringLength(100)]
    public string? Category { get; set; }

    /// <summary>
    /// Gender classification (male, female, unisex).
    /// </summary>
    [StringLength(20)]
    public string? Gender { get; set; }

    /// <summary>
    /// Indicates whether the product is currently active.
    /// </summary>
    public bool? CurrentlyActive { get; set; }
}

