namespace WebShop.Business.DTOs;

/// <summary>
/// Summary of a user's permission access: which actions they can perform (e.g. view, create).
/// </summary>
public class AsmPermissionDto
{
    /// <summary>Actions the user can perform in this module (e.g. SHR:VIEW, SHR:CREATE).</summary>
    public IReadOnlyList<string> Permissions { get; set; } = new List<string>();
}
