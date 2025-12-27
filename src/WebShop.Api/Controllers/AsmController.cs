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
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A list of application security information (permissions, access rights) for the current user.</returns>
    /// <remarks>
    /// This endpoint retrieves application security management (ASM) information for the currently authenticated user.
    /// The security information is based on the user's roles and positions in the Management Information System (MIS).
    /// The user ID and access token are extracted from the JWT token in the Authorization header.
    /// Returns 401 Unauthorized if the user ID or token is not available in the context.
    /// Returns an empty list (200 OK) if no security information is found for the user.
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

        IReadOnlyList<AsmResponseDto> securityInfo = await _asmService.GetApplicationSecurityAsync(personId, token, cancellationToken);

        if (securityInfo.Count == 0)
        {
            string message = $"No application security found for person ID: {personId}";
            _logger.LogWarning(message);
            return Ok(Response<IReadOnlyList<AsmResponseDto>>.Success(securityInfo, message));
        }

        return Ok(Response<IReadOnlyList<AsmResponseDto>>.Success(securityInfo, "Application security retrieved successfully"));
    }
}

