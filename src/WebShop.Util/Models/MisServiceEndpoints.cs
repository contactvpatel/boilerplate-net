using System.ComponentModel.DataAnnotations;

namespace WebShop.Util.Models;

/// <summary>
/// MIS service endpoint paths.
/// </summary>
public class MisServiceEndpoints
{
    /// <summary>
    /// Endpoint for geo level/division.
    /// </summary>
    [Required]
    public string GeoLevel { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for departments.
    /// </summary>
    [Required]
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for entities.
    /// </summary>
    [Required]
    public string Entity { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for role types.
    /// </summary>
    [Required]
    public string RoleType { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for roles.
    /// </summary>
    [Required]
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for positions.
    /// </summary>
    [Required]
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for person positions.
    /// </summary>
    [Required]
    public string PersonPosition { get; set; } = string.Empty;
}
