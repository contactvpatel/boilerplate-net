using System.IdentityModel.Tokens.Jwt;
using System.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WebShop.Api.Models;
using WebShop.Core.Interfaces.Base;
using WebShop.Util.Security;
using ISsoService = WebShop.Business.Services.Interfaces.ISsoService;

namespace WebShop.Api.Filters;

/// <summary>
/// Authentication filter that validates JWT tokens using SSO service.
/// This filter runs before controller actions and validates Bearer token in the Authorization header.
/// Validated tokens are cached until their expiration time to avoid repeated SSO service calls.
/// </summary>
public class JwtTokenAuthenticationFilter(
    ISsoService ssoService,
    IHttpContextAccessor httpContextAccessor,
    ICacheService cacheService,
    ILogger<JwtTokenAuthenticationFilter> logger) : IAsyncAuthorizationFilter
{
    private readonly ISsoService _ssoService = ssoService ?? throw new ArgumentNullException(nameof(ssoService));
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    private readonly ICacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly ILogger<JwtTokenAuthenticationFilter> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Validates the JWT token and extracts user information if valid.
    /// </summary>
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        if (context == null)
        {
            return;
        }

        // Skip authentication if the endpoint has [AllowAnonymous] attribute
        if (HasAllowAnonymousAttribute(context))
        {
            return;
        }

        // Extract and validate token
        string? token = ExtractTokenFromHeader(context);
        if (string.IsNullOrWhiteSpace(token))
        {
            LogAndSetUnauthenticatedResponse(context, "Token is missing or invalid",
                context.HttpContext.Request.Path, context.HttpContext.Request.Method);
            return;
        }

        // Parse and validate token structure
        JwtSecurityToken? jwtToken = JwtTokenHelper.ParseToken(token);
        if (jwtToken == null)
        {
            LogAndSetUnauthenticatedResponse(context, "Invalid token format",
                context.HttpContext.Request.Path);
            return;
        }

        // Check if token is expired
        if (JwtTokenHelper.IsTokenExpired(jwtToken))
        {
            LogAndSetUnauthenticatedResponse(context, "Token has expired",
                context.HttpContext.Request.Path);
            return;
        }

        // Validate token with caching
        bool isValid = await ValidateTokenWithCacheAsync(token, jwtToken, context);
        if (!isValid)
        {
            string? userId = JwtTokenHelper.GetUserId(jwtToken);
            _logger.LogWarning(
                "Unauthorized access attempt: Token validation failed. Path: {Path}, UserId: {UserId}",
                context.HttpContext.Request.Path, userId ?? "Unknown");

            SetUnauthenticatedResponse(context, "Token validation failed");
            return;
        }

        // Store user context for use in controllers/services
        StoreUserContext(token, jwtToken);

        _logger.LogDebug("Token validated successfully. Path: {Path}",
            context.HttpContext.Request.Path);
    }

    /// <summary>
    /// Checks if the endpoint has the [AllowAnonymous] attribute.
    /// </summary>
    private static bool HasAllowAnonymousAttribute(AuthorizationFilterContext context)
    {
        return context.ActionDescriptor.EndpointMetadata
            .Any(metadata => metadata is AllowAnonymousAttribute);
    }

    /// <summary>
    /// Extracts the Bearer token from the Authorization header.
    /// </summary>
    private static string? ExtractTokenFromHeader(AuthorizationFilterContext context)
    {
        string? authorizationHeader = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();
        return JwtTokenHelper.ExtractBearerToken(authorizationHeader);
    }

    /// <summary>
    /// Validates the token using cache-first strategy with SSO service.
    /// </summary>
    private async Task<bool> ValidateTokenWithCacheAsync(
        string token,
        JwtSecurityToken jwtToken,
        AuthorizationFilterContext context)
    {
        try
        {
            TimeSpan? cacheExpiration = JwtTokenHelper.GetCacheExpiration(jwtToken);

            // Generate cache key from token hash (for security, don't store full token)
            string cacheKey = JwtTokenHelper.GenerateCacheKey(token);

            // Check cache first, then validate with SSO service if not cached
            bool isValid = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async cancellationToken =>
                {
                    // Token not in cache, validate with SSO service
                    _logger.LogDebug("Token not in cache, validating with SSO service. Path: {Path}",
                        context.HttpContext.Request.Path);

                    return await _ssoService.ValidateTokenAsync(token, cancellationToken);
                },
                expiration: cacheExpiration,
                cancellationToken: context.HttpContext.RequestAborted);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error during token validation with cache. Path: {Path}",
                context.HttpContext.Request.Path);

            // On cache error, fall back to direct SSO validation (fail-open for availability)
            try
            {
                return await _ssoService.ValidateTokenAsync(token, context.HttpContext.RequestAborted);
            }
            catch (Exception fallbackEx)
            {
                _logger.LogError(fallbackEx,
                    "Error during fallback token validation. Path: {Path}",
                    context.HttpContext.Request.Path);
                return false;
            }
        }
    }

    /// <summary>
    /// Stores user information in HTTP context for use in controllers/services.
    /// </summary>
    private void StoreUserContext(string token, JwtSecurityToken jwtToken)
    {
        string? userId = JwtTokenHelper.GetUserId(jwtToken);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            _httpContextAccessor.HttpContext?.Items.TryAdd("UserId", userId);
            _logger.LogDebug("User authenticated successfully. UserId: {UserId}", userId);
        }

        _httpContextAccessor.HttpContext?.Items.TryAdd("UserToken", token);
    }

    /// <summary>
    /// Logs a warning and sets the unauthenticated response.
    /// </summary>
    private void LogAndSetUnauthenticatedResponse(
        AuthorizationFilterContext context,
        string reason,
        string path,
        string? method = null)
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            _logger.LogWarning(
                "Unauthorized access attempt: {Reason}. Path: {Path}",
                reason, path);
        }
        else
        {
            _logger.LogWarning(
                "Unauthorized access attempt: {Reason}. Path: {Path}, Method: {Method}",
                reason, path, method);
        }

        SetUnauthenticatedResponse(context, reason);
    }

    /// <summary>
    /// Sets the unauthenticated response on the authorization filter context using the standard Response model.
    /// </summary>
    private static void SetUnauthenticatedResponse(AuthorizationFilterContext context, string reason)
    {
        List<ApiError> errors = new()
        {
            new ApiError
            {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = (short)HttpStatusCode.Unauthorized,
                Message = reason
            }
        };

        Response<object?> response = new(null, false, "Authentication failed", errors);
        context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
        context.Result = new UnauthorizedObjectResult(response);
    }
}

