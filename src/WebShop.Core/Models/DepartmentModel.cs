namespace WebShop.Core.Models;

/// <summary>
/// Model representing a department from the MIS service.
/// </summary>
public class DepartmentModel
{
    /// <summary>
    /// Department identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Department name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Division identifier this department belongs to.
    /// </summary>
    public int DivisionId { get; set; }

    /// <summary>
    /// Additional department information.
    /// </summary>
    public string? Description { get; set; }
}

