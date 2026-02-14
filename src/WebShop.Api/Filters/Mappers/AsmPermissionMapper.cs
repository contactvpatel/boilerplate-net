using WebShop.Business.DTOs;

namespace WebShop.Api.Filters.Mappers;

/// <summary>
/// Maps ASM service response to permission DTOs for authorization decisions.
/// </summary>
public sealed class AsmPermissionMapper : IAsmPermissionMapper
{
    /// <inheritdoc />
    public IReadOnlyList<AsmPermissionDto> MapToPermissions(IReadOnlyList<AsmResponseDto> asmResponseList)
    {
        if (asmResponseList == null || asmResponseList.Count == 0)
        {
            return [];
        }

        List<AsmPermissionDto> list = [];
        foreach (AsmResponseDto item in asmResponseList)
        {
            if (item.ApplicationAccess == null)
            {
                continue;
            }

            foreach (ApplicationAccessDto access in item.ApplicationAccess)
            {
                List<string> permissions = [];
                string code = access.ModuleCode ?? string.Empty;
                if (access.HasViewAccess == true)
                {
                    permissions.Add($"{code}:VIEW");
                }
                if (access.HasCreateAccess == true)
                {
                    permissions.Add($"{code}:CREATE");
                }
                if (access.HasUpdateAccess == true)
                {
                    permissions.Add($"{code}:UPDATE");
                }
                if (access.HasDeleteAccess == true)
                {
                    permissions.Add($"{code}:DELETE");
                }
                if (access.HasAccess == true && permissions.Count == 0)
                {
                    permissions.Add($"{code}:ACCESS");
                }

                list.Add(new AsmPermissionDto
                {
                    Permissions = permissions
                });
            }
        }

        return list;
    }
}
