using Microsoft.AspNetCore.Mvc.Filters;
using WebShop.Api.Filters.Factories;
using WebShop.Api.Filters.Validators;
using WebShop.Business.DTOs;
using WebShop.Core.Interfaces.Base;
using IAsmService = WebShop.Business.Services.Interfaces.IAsmService;

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
    private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    private readonly IAsmService _asmService = asmService ?? throw new ArgumentNullException(nameof(asmService));
    private readonly IUserContext _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    private readonly ILogger<AsmAuthorizationValidation> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IAsmPermissionValidator _permissionValidator = permissionValidator ?? throw new ArgumentNullException(nameof(permissionValidator));
    private readonly IAsmErrorResponseFactory _errorResponseFactory = errorResponseFactory ?? throw new ArgumentNullException(nameof(errorResponseFactory));
    private readonly PermissionRequirement[] _permissionRequirements = permissionRequirements ?? throw new ArgumentNullException(nameof(permissionRequirements));
    private readonly LogicalOperator _logicalOperator = logicalOperator;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        // Skip authorization if disabled in configuration
        if (!_configuration.GetValue<bool>("EnableAsmAuthorization"))
        {
            await next();
            return;
        }

        // Check if user context is available
        string? userId = _userContext.GetUserId();
        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("ASM Authorization failed: No user context available");
            context.Result = _errorResponseFactory.CreateUnauthorizedResponse("Authentication required");
            return;
        }

        try
        {
            // Get user permissions from ASM service
            string? token = _userContext.GetToken();
            CancellationToken cancellationToken = context.HttpContext.RequestAborted;

            IReadOnlyList<AsmResponseDto> asmResponseList = await _asmService.GetApplicationSecurityAsync(
                userId, token ?? string.Empty, cancellationToken);

            List<AsmPermissionDto> asmPermissionList = MapToAsmPermissionDto(asmResponseList);

            if (asmPermissionList.Count == 0)
            {
                _logger.LogWarning("ASM Authorization failed: User {UserId} has no assigned permissions",
                    userId);
                context.Result = _errorResponseFactory.CreateForbiddenResponse("No permissions assigned to user");
                return;
            }

            // Check if user has required permissions based on logical operator
            bool hasRequiredPermissions = _permissionValidator.ValidatePermissions(
                asmPermissionList, _permissionRequirements, _logicalOperator);

            if (!hasRequiredPermissions)
            {
                string requiredPermissions = string.Join(", ",
                    _permissionRequirements.Select(p => $"{p.ModuleCode}:{p.AccessType}"));
                _logger.LogWarning(
                    "ASM Authorization failed: User {UserId} lacks required permissions. Required: {RequiredPermissions}, Operator: {Operator}",
                    userId, requiredPermissions, _logicalOperator);
                context.Result = _errorResponseFactory.CreateForbiddenResponse("Insufficient permissions for this operation");
                return;
            }

            _logger.LogDebug("ASM Authorization successful: User {UserId} has required permissions",
                userId);
            await next();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ASM Authorization error for user {UserId}: {Message}",
                userId, ex.Message);
            context.Result = _errorResponseFactory.CreateInternalServerErrorResponse("Authorization service temporarily unavailable");
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
