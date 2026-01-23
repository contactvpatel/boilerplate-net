using WebShop.Api.Extensions.Utilities;
using WebShop.Business.DTOs;

namespace WebShop.Api.Filters.Validators;

/// <summary>
/// Validator for ASM permissions with support for OR and AND logical operators.
/// </summary>
public class AsmPermissionValidator : IAsmPermissionValidator
{
    public bool ValidatePermissions(
        IReadOnlyList<AsmResponseDto> asmPermissions,
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
    /// Validates permissions with OR logic - user must have at least one of the required permissions.
    /// </summary>
    private static bool ValidateWithOr(IReadOnlyList<AsmResponseDto> asmPermissions, PermissionRequirement[] permissionRequirements)
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
    /// Validates permissions with AND logic - user must have all of the required permissions.
    /// </summary>
    private static bool ValidateWithAnd(IReadOnlyList<AsmResponseDto> asmPermissions, PermissionRequirement[] permissionRequirements)
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
    /// Checks if the user has a specific permission.
    /// </summary>
    private static bool HasPermission(PermissionRequirement requirement, IReadOnlyList<AsmResponseDto> asmPermissions)
    {
        string moduleDescription = requirement.ModuleCode.GetDescription();

        return requirement.AccessType switch
        {
            AccessType.View => asmPermissions.Any(p =>
                p.HasAccess && p.Permissions.Contains($"{moduleDescription}:VIEW")),
            AccessType.Create => asmPermissions.Any(p =>
                p.HasAccess && p.Permissions.Contains($"{moduleDescription}:CREATE")),
            AccessType.Update => asmPermissions.Any(p =>
                p.HasAccess && p.Permissions.Contains($"{moduleDescription}:UPDATE")),
            AccessType.Delete => asmPermissions.Any(p =>
                p.HasAccess && p.Permissions.Contains($"{moduleDescription}:DELETE")),
            AccessType.Access => asmPermissions.Any(p =>
                p.HasAccess && p.Permissions.Contains($"{moduleDescription}:ACCESS")),
            AccessType.AllowAny => true,
            _ => false
        };
    }
}
