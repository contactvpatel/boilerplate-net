using System.IdentityModel.Tokens.Jwt;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Core.Interfaces.Base;
using WebShop.Util.Security;
using ISsoService = WebShop.Business.Services.Interfaces.ISsoService;

namespace WebShop.Api.Controllers;

/// <summary>
/// SSO Controller for Single Sign-On operations.
/// Provides endpoints for token renewal and user logout.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/sso")]
[Produces("application/json")]
public class SsoController(
    ISsoService ssoService,
    IUserContext userContext,
    ICacheService cacheService,
    ILogger<SsoController> logger) : BaseApiController
{
    private readonly ISsoService _ssoService = ssoService ?? throw new ArgumentNullException(nameof(ssoService));
    private readonly IUserContext _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    private readonly ICacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly ILogger<SsoController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Renews an authentication token using a refresh token.
    /// </summary>
    /// <param name="request">The token renewal request containing the refresh token.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>New authentication information (access token and refresh token) if successful, otherwise 401 Unauthorized.</returns>
    /// <remarks>
    /// This endpoint allows clients to obtain a new access token using a valid refresh token. The current access token from the Authorization header
    /// and the refresh token from the request body are both required. This endpoint is publicly accessible (no authentication required).
    /// If the refresh token is invalid or expired, a 401 Unauthorized response is returned.
    /// </remarks>
    /// <example>
    /// <code>
    /// POST /api/v1/sso/renew-token
    /// Authorization: Bearer {current_access_token}
    /// {
    ///   "refreshToken": "refresh_token_here"
    /// }
    /// </code>
    /// </example>
    [HttpPost("renew-token")]
    [AllowAnonymous]
    [EnableRateLimiting("strict")] // Apply strict rate limiting to prevent brute force attacks
    [ProducesResponseType(typeof(Response<SsoAuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<SsoAuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Response<SsoAuthResponse>>> RenewToken(
        [FromBody] SsoRenewTokenRequest request,
        CancellationToken cancellationToken)
    {
        string? token = _userContext.GetToken();

        if (!string.IsNullOrWhiteSpace(token) && !string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            SsoAuthResponse? authResponse = await _ssoService.RenewTokenAsync(token, request.RefreshToken, cancellationToken);

            if (authResponse != null)
            {
                return Ok(Response<SsoAuthResponse>.Success(authResponse, "Token renewed successfully"));
            }
        }

        return UnauthorizedResponse<SsoAuthResponse>("Token renewal failed", "Renew token failed - Access token or refresh token is missing");
    }

    /// <summary>
    /// Logs out the current user and invalidates their token.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>Success response if logout was successful, otherwise 401 Unauthorized.</returns>
    /// <remarks>
    /// This endpoint logs out the current authenticated user. It performs the following actions:
    /// <list type="bullet">
    /// <item><description>If the token is still valid, calls the SSO service to invalidate the token on the SSO server</description></item>
    /// <item><description>If the token is already expired, skips the SSO API call to avoid unnecessary network traffic</description></item>
    /// <item><description>Clears the cached JWT token validation result from the local cache</description></item>
    /// <item><description>Clears all cached user-specific data (positions, ASM authorization, etc.)</description></item>
    /// </list>
    /// This endpoint is publicly accessible (no authentication required) to allow logout even with expired tokens.
    /// </remarks>
    /// <example>
    /// <code>
    /// POST /api/v1/sso/logout
    /// Authorization: Bearer {access_token}
    /// </code>
    /// </example>
    [HttpPost("logout")]
    [AllowAnonymous]
    [EnableRateLimiting("strict")] // Apply strict rate limiting to prevent abuse
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<Response<bool>>> Logout(CancellationToken cancellationToken)
    {
        string? token = _userContext.GetToken();
        string? userId = _userContext.GetUserId();

        if (!string.IsNullOrWhiteSpace(token))
        {
            // Check if token is already expired
            JwtSecurityToken? jwtToken = JwtTokenHelper.ParseToken(token);
            bool isExpired = jwtToken != null && JwtTokenHelper.IsTokenExpired(jwtToken);

            bool logoutSuccess = true;

            if (!isExpired)
            {
                // Token is still valid, call SSO API to logout
                logoutSuccess = await _ssoService.LogoutAsync(token, cancellationToken);
            }
            // Token is already expired, skip SSO API call (no logging needed)

            if (logoutSuccess)
            {
                // Clear cached JWT token validation result
                string tokenCacheKey = JwtTokenHelper.GenerateCacheKey(token);
                await _cacheService.RemoveAsync(tokenCacheKey, cancellationToken);

                // Clear cached data for this user
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    await _cacheService.RemoveAsync($"person-{userId}-positions", cancellationToken);
                    await _cacheService.RemoveAsync($"asm-authorization-{userId}", cancellationToken);
                }

                return Ok(Response<bool>.Success(true, "Logout completed successfully"));
            }
        }

        return UnauthorizedResponse<bool>("Logout failed", "Logout failed");
    }
}

