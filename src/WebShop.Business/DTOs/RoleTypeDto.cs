namespace WebShop.Business.DTOs;

/// <summary>
/// Data transfer object representing a role type from the MIS service.
/// </summary>
public class RoleTypeDto
{
    /// <summary>
    /// Role type identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Role type name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Division identifier this role type belongs to.
    /// </summary>
    public int DivisionId { get; set; }
}

