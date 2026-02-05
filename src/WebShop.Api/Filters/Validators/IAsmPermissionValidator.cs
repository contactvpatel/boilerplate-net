using WebShop.Business.DTOs;

namespace WebShop.Api.Filters.Validators;

/// <summary>
/// Used to decide whether a user is allowed to perform an action based on their application permissions.
/// </summary>
public interface IAsmPermissionValidator
{
    /// <summary>
    /// Returns whether the user has the required permissions (either any one or all, depending on the operator).
    /// </summary>
    /// <param name="asmPermissions">The user's assigned application permissions.</param>
    /// <param name="permissionRequirements">The permissions required for this action.</param>
    /// <param name="logicalOperator">Whether the user must have any one or all of the required permissions.</param>
    /// <returns>True if the user is allowed, false otherwise.</returns>
    bool ValidatePermissions(
        IReadOnlyList<AsmPermissionDto> asmPermissions,
        PermissionRequirement[] permissionRequirements,
        LogicalOperator logicalOperator);
}
