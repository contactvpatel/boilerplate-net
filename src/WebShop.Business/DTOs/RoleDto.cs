namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object representing a role from the MIS service.
/// </summary>
public class RoleDto
{
    /// <summary>
    /// Role identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Role name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Department identifier this role belongs to.
    /// </summary>
    public int DepartmentId { get; set; }

    /// <summary>
    /// Role type identifier.
    /// </summary>
    public int RoleTypeId { get; set; }

    /// <summary>
    /// Division identifier.
    /// </summary>
    public int DivisionId { get; set; }
}

