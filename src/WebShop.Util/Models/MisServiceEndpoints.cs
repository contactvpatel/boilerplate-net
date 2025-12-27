namespace WebShop.Util.Models;

/// <summary>
/// MIS service endpoint paths.
/// </summary>
public class MisServiceEndpoints
{
    /// <summary>
    /// Endpoint for geo level/division.
    /// </summary>
    public string GeoLevel { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for departments.
    /// </summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for entities.
    /// </summary>
    public string Entity { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for role types.
    /// </summary>
    public string RoleType { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for roles.
    /// </summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for positions.
    /// </summary>
    public string Position { get; set; } = string.Empty;

    /// <summary>
    /// Endpoint for person positions.
    /// </summary>
    public string PersonPosition { get; set; } = string.Empty;
}
