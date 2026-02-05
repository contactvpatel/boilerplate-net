namespace WebShop.Core.Models;

/// <summary>
/// One combination of role and position with the list of applications and actions the user can use in that context.
/// </summary>
public class AsmResponseModel
{
    /// <summary>Identifier of the role in the organization.</summary>
    public int RoleId { get; set; }

    /// <summary>Identifier of the position in the organization.</summary>
    public int PositionId { get; set; }

    /// <summary>Applications access for the user based on this role/position (e.g. view, create per module).</summary>
    public List<ApplicationAccessModel> ApplicationAccess { get; set; } = new();
}
