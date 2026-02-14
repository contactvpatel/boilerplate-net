using WebShop.Business.DTOs;

namespace WebShop.Api.Filters.Mappers;

/// <summary>
/// Maps ASM service response to permission DTOs for authorization decisions.
/// </summary>
public interface IAsmPermissionMapper
{
    /// <summary>
    /// Builds the list of a user's module permissions (e.g. view, create per module).
    /// </summary>
    /// <param name="asmResponseList">Raw response from ASM service.</param>
    /// <returns>List of permission DTOs used to decide if the user can perform the action.</returns>
    IReadOnlyList<AsmPermissionDto> MapToPermissions(IReadOnlyList<AsmResponseDto> asmResponseList);
}
