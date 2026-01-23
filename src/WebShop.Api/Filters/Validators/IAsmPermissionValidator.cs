using WebShop.Business.DTOs;

namespace WebShop.Api.Filters.Validators;

/// <summary>
/// Interface for validating ASM permissions.
/// </summary>
public interface IAsmPermissionValidator
{
    /// <summary>
    /// Validates if the user has the required permissions based on the logical operator.
    /// </summary>
    /// <param name="asmPermissions">The user's ASM permissions.</param>
    /// <param name="permissionRequirements">The permission requirements to check.</param>
    /// <param name="logicalOperator">The logical operator (OR or AND).</param>
    /// <returns>True if the user has the required permissions, false otherwise.</returns>
    bool ValidatePermissions(
        IReadOnlyList<AsmResponseDto> asmPermissions,
        PermissionRequirement[] permissionRequirements,
        LogicalOperator logicalOperator);
}
