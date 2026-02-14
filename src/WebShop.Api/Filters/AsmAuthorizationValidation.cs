using Microsoft.AspNetCore.Mvc.Filters;
using WebShop.Api.Filters.Factories;
using WebShop.Api.Filters.Mappers;
using WebShop.Api.Filters.Validators;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;
using WebShop.Core.Interfaces.Base;
using WebShop.Util;

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
    IAsmPermissionMapper permissionMapper,
    IAsmPermissionValidator permissionValidator,
    IAsmErrorResponseFactory errorResponseFactory,
    PermissionRequirement[] permissionRequirements,
    LogicalOperator logicalOperator) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Skip authorization if disabled in configuration
        if (!configuration.GetValue<bool>(ConfigurationKeys.AppSettingsEnableAsmAuthorization))
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

            IReadOnlyList<AsmPermissionDto> asmPermissionList = permissionMapper.MapToPermissions(asmResponseList);

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
}
