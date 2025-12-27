namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object representing a position from the MIS service.
/// </summary>
public class PositionDto
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

