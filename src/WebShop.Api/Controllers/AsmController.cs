using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Api.Controllers;

/// <summary>
/// ASM Controller for Application Security Management (ASM) operations.
/// Provides endpoints for retrieving application security information based on user roles and positions.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/asm")]
[Produces("application/json")]
public class AsmController(IAsmService asmService, IUserContext userContext, ILogger<AsmController> logger) : BaseApiController
{
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
        string? personId = userContext.GetUserId();
        string? token = userContext.GetToken();

        if (string.IsNullOrWhiteSpace(personId) || string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("Person ID or token not available in context");
            return UnauthorizedResponse<IReadOnlyList<AsmResponseDto>>("Person ID or token not available", "Person ID or token not available");
        }

        IReadOnlyList<AsmResponseDto> accessPermissions = await asmService.GetApplicationSecurityAsync(personId, token, cancellationToken);

        if (accessPermissions.Count == 0)
        {
            string message = $"No application security found for Person ID: {personId}";
            logger.LogWarning(message);
            return Ok(Response<IReadOnlyList<AsmResponseDto>>.Success(accessPermissions, message));
        }

        return Ok(Response<IReadOnlyList<AsmResponseDto>>.Success(accessPermissions, "Application security retrieved successfully"));
    }
}

