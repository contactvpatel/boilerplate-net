using WebShop.Api.Extensions.Utilities;
using WebShop.Business.DTOs;

namespace WebShop.Api.Filters.Validators;

/// <summary>
/// Decides whether a user is allowed to perform an action, based on their permissions and whether any or all required permissions are needed.
/// </summary>
public class AsmPermissionValidator : IAsmPermissionValidator
{
    public bool ValidatePermissions(
        IReadOnlyList<AsmPermissionDto> asmPermissions,
        PermissionRequirement[] permissionRequirements,
        LogicalOperator logicalOperator)
    {
        // AllowAny access type always passes
        if (permissionRequirements.Any(p => p.AccessType == AccessType.AllowAny))
        {
            return true;
        }

        return logicalOperator switch
        {
            LogicalOperator.OR => ValidateWithOr(asmPermissions, permissionRequirements),
            LogicalOperator.AND => ValidateWithAnd(asmPermissions, permissionRequirements),
            _ => false
        };
    }

    /// <summary>
    /// User is allowed if they have at least one of the required permissions.
    /// </summary>
    private static bool ValidateWithOr(IReadOnlyList<AsmPermissionDto> asmPermissions, PermissionRequirement[] permissionRequirements)
    {
        foreach (PermissionRequirement requirement in permissionRequirements)
        {
            if (HasPermission(requirement, asmPermissions))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// User is allowed only if they have every required permission.
    /// </summary>
    private static bool ValidateWithAnd(IReadOnlyList<AsmPermissionDto> asmPermissions, PermissionRequirement[] permissionRequirements)
    {
        foreach (PermissionRequirement requirement in permissionRequirements)
        {
            if (!HasPermission(requirement, asmPermissions))
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Determines whether the user has a specific permission (e.g. view, create) for the required application.
    /// </summary>
    private static bool HasPermission(PermissionRequirement requirement, IReadOnlyList<AsmPermissionDto> asmPermissions)
    {
        string moduleDescription = requirement.ModuleCode.GetDescription();

        return requirement.AccessType switch
        {
            AccessType.View => asmPermissions.Any(p =>
                p.Permissions.Contains($"{moduleDescription}:VIEW")),
            AccessType.Create => asmPermissions.Any(p =>
                p.Permissions.Contains($"{moduleDescription}:CREATE")),
            AccessType.Update => asmPermissions.Any(p =>
                p.Permissions.Contains($"{moduleDescription}:UPDATE")),
            AccessType.Delete => asmPermissions.Any(p =>
                p.Permissions.Contains($"{moduleDescription}:DELETE")),
            AccessType.Access => asmPermissions.Any(p =>
                p.Permissions.Contains($"{moduleDescription}:ACCESS")),
            AccessType.AllowAny => true,
            _ => false
        };
    }
}
