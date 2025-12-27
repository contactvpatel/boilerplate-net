namespace WebShop.Core.Models;

/// <summary>
/// Model representing a position from the MIS service.
/// </summary>
public class PositionModel
{
    /// <summary>
    /// Position identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Position name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Role identifier this position belongs to.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Additional position information.
    /// </summary>
    public string? Description { get; set; }
}

