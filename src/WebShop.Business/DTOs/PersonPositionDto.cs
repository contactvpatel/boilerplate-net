namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object representing a person's position from the MIS service.
/// </summary>
public class PersonPositionDto
{
    /// <summary>
    /// Person identifier.
    /// </summary>
    public string PersonId { get; set; } = string.Empty;

    /// <summary>
    /// Position identifier.
    /// </summary>
    public int PositionId { get; set; }

    /// <summary>
    /// Position name.
    /// </summary>
    public string PositionName { get; set; } = string.Empty;

    /// <summary>
    /// Role identifier.
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Role name.
    /// </summary>
    public string RoleName { get; set; } = string.Empty;

    /// <summary>
    /// Department identifier.
    /// </summary>
    public int DepartmentId { get; set; }

    /// <summary>
    /// Department name.
    /// </summary>
    public string DepartmentName { get; set; } = string.Empty;

    /// <summary>
    /// Division identifier.
    /// </summary>
    public int DivisionId { get; set; }
}

