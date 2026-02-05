using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Core.Interfaces.Base;
using IAsmService = WebShop.Business.Services.Interfaces.IAsmService;

namespace WebShop.Api.Controllers;

/// <summary>
/// ASM Controller for Application Security Management (ASM) operations.
/// Provides endpoints for retrieving application security information based on user roles and positions.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/asm")]
[Produces("application/json")]
public class AsmController(
    IAsmService asmService,
    IUserContext userContext,
    ILogger<AsmController> logger) : BaseApiController
{
    private readonly IAsmService _asmService = asmService ?? throw new ArgumentNullException(nameof(asmService));
    private readonly IUserContext _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    private readonly ILogger<AsmController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Gets application security information for the current authenticated user based on their roles and positions.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of application security information (permissions, access rights) for the current user.</returns>
    /// <remarks>
    /// Shows what the current user can access. Requires a valid login. Returns an empty list if the user has no assigned access.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/asm
    /// Authorization: Bearer {access_token}
    /// </code>
    /// </example>
    [HttpGet]
    [ProducesResponseType(typeof(Response<IReadOnlyList<AsmResponseDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<AsmResponseDto>>>> Get(CancellationToken cancellationToken)
    {
        string? personId = _userContext.GetUserId();
        string? token = _userContext.GetToken();

        if (string.IsNullOrWhiteSpace(personId) || string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Person ID or token not available in context");
            return UnauthorizedResponse<IReadOnlyList<AsmResponseDto>>("Person ID or token not available", "Person ID or token not available");
        }

        IReadOnlyList<AsmResponseDto> accessPermissions = await _asmService.GetApplicationSecurityAsync(personId, token, cancellationToken);

        if (accessPermissions.Count == 0)
        {
            string message = $"No application security found for Person ID: {personId}";
            _logger.LogWarning(message);
            return Ok(Response<IReadOnlyList<AsmResponseDto>>.Success(accessPermissions, message));
        }

        return Ok(Response<IReadOnlyList<AsmResponseDto>>.Success(accessPermissions, "Application security retrieved successfully"));
    }
}

