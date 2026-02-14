using Microsoft.AspNetCore.Mvc.Filters;
using WebShop.Api.Filters.Factories;
using WebShop.Api.Filters.Validators;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Api.Filters;

/// <summary>
/// Ensures only users with the required application permissions can perform the action (e.g. view, create, update).
/// Supports requiring any one permission (OR) or all permissions (AND).
/// </summary>
public class AsmAuthorizationValidation(
    IConfiguration configuration,
    IAsmService asmService,
    IUserContext userContext,
    ILogger<AsmAuthorizationValidation> logger,
    IAsmPermissionValidator permissionValidator,
    IAsmErrorResponseFactory errorResponseFactory,
    PermissionRequirement[] permissionRequirements,
    LogicalOperator logicalOperator) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Skip authorization if disabled in configuration
        if (!configuration.GetValue<bool>("EnableAsmAuthorization"))
        {
            await next();
            return;
        }

        // Check if user context is available
        string? userId = userContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            logger.LogWarning("ASM Authorization failed: No user context available");
            context.Result = errorResponseFactory.CreateUnauthorizedResponse("Authentication required");
            return;
        }

        try
        {
            // Get user permissions from ASM service
            string? token = userContext.GetToken();
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            IReadOnlyList<AsmResponseDto> asmResponseList = await asmService.GetApplicationSecurityAsync(
                userId, token ?? string.Empty, cancellationToken);

            List<AsmPermissionDto> asmPermissionList = MapToAsmPermissionDto(asmResponseList);

            if (asmPermissionList.Count == 0)
            {
                logger.LogWarning("ASM Authorization failed: User {UserId} has no assigned permissions",
                    userId);
                context.Result = errorResponseFactory.CreateForbiddenResponse("No permissions assigned to user");
                return;
            }

            // Check if user has required permissions based on logical operator
            bool hasRequiredPermissions = permissionValidator.ValidatePermissions(
                asmPermissionList, permissionRequirements, logicalOperator);

            if (!hasRequiredPermissions)
            {
                string requiredPermissions = string.Join(", ",
                    permissionRequirements.Select(p => $"{p.ModuleCode}:{p.AccessType}"));
                logger.LogWarning(
                    "ASM Authorization failed: User {UserId} lacks required permissions. Required: {RequiredPermissions}, Operator: {Operator}",
                    userId, requiredPermissions, logicalOperator);
                context.Result = errorResponseFactory.CreateForbiddenResponse("Insufficient permissions for this operation");
                return;
            }

            logger.LogDebug("ASM Authorization successful: User {UserId} has required permissions",
                userId);
            await next();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "ASM Authorization error for user {UserId}: {Message}",
                userId, ex.Message);
            context.Result = errorResponseFactory.CreateInternalServerErrorResponse("Authorization service temporarily unavailable");
        }
    }

    /// <summary>
    /// Builds the list of a user's module permissions (e.g. view, create per module) used to decide if they can perform the action.
    /// </summary>
    private static List<AsmPermissionDto> MapToAsmPermissionDto(IReadOnlyList<AsmResponseDto> asmResponseList)
    {
        if (asmResponseList == null || asmResponseList.Count == 0)
        {
            return [];
        }

        List<AsmPermissionDto> list = new List<AsmPermissionDto>();
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
