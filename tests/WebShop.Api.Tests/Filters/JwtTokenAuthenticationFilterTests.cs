using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Filters;
using WebShop.Api.Models;
using WebShop.Core.Interfaces.Base;
using WebShop.Util.Security;
using Xunit;
using ISsoService = WebShop.Business.Services.Interfaces.ISsoService;

namespace WebShop.Api.Tests.Filters;

/// <summary>
/// Unit tests for JwtTokenAuthenticationFilter.
/// </summary>
[Trait("Category", "Unit")]
public class JwtTokenAuthenticationFilterTests
{
    private readonly Mock<ISsoService> _mockSsoService;
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<JwtTokenAuthenticationFilter>> _mockLogger;
    private readonly JwtTokenAuthenticationFilter _filter;

    public JwtTokenAuthenticationFilterTests()
    {
        _mockSsoService = new Mock<ISsoService>();
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<JwtTokenAuthenticationFilter>>();
        _filter = new JwtTokenAuthenticationFilter(
            _mockSsoService.Object,
            _mockHttpContextAccessor.Object,
            _mockCacheService.Object,
            _mockLogger.Object);
    }

    #region OnAuthorizationAsync Tests

    [Fact]
    public async Task OnAuthorizationAsync_NullContext_DoesNotThrow()
    {
        Func<Task> act = async () => await _filter.OnAuthorizationAsync(null!);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnAuthorizationAsync_AllowAnonymousAttribute_AllowsAccess()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        ActionDescriptor actionDescriptor = new();
        // Create a new list with AllowAnonymousAttribute since EndpointMetadata is fixed-size
        List<object> metadata = new() { new AllowAnonymousAttribute() };
        actionDescriptor.EndpointMetadata = metadata;
        ActionContext actionContext = new(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), actionDescriptor);
        AuthorizationFilterContext context = new(actionContext, new List<IFilterMetadata>());

        // Act
        await _filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull();
        _mockSsoService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnAuthorizationAsync_MissingToken_ReturnsUnauthorized()
    {
        // Arrange
        AuthorizationFilterContext context = CreateAuthorizationFilterContext();

        // Act
        await _filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>();
        UnauthorizedObjectResult? result = context.Result as UnauthorizedObjectResult;
        result?.Value.Should().BeOfType<Response<object?>>();
        Response<object?>? response = result?.Value as Response<object?>;
        response?.Succeeded.Should().BeFalse();
        response?.Message.Should().Contain("Authentication failed");
    }

    [Fact]
    public async Task OnAuthorizationAsync_InvalidTokenFormat_ReturnsUnauthorized()
    {
        // Arrange
        AuthorizationFilterContext context = CreateAuthorizationFilterContext();
        context.HttpContext.Request.Headers.Authorization = "Bearer invalid-token-format";

        // Act
        await _filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>();
        UnauthorizedObjectResult? result = context.Result as UnauthorizedObjectResult;
        result?.Value.Should().BeOfType<Response<object?>>();
    }

    [Fact]
    public async Task OnAuthorizationAsync_ExpiredToken_ReturnsUnauthorized()
    {
        // Arrange
        string expiredToken = CreateExpiredToken();
        AuthorizationFilterContext context = CreateAuthorizationFilterContext();
        context.HttpContext.Request.Headers.Authorization = $"Bearer {expiredToken}";

        // Act
        await _filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task OnAuthorizationAsync_ValidToken_AllowsAccess()
    {
        // Arrange
        string validToken = CreateValidToken();
        AuthorizationFilterContext context = CreateAuthorizationFilterContext();
        context.HttpContext.Request.Headers.Authorization = $"Bearer {validToken}";

        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<bool>>, TimeSpan?, TimeSpan?, CancellationToken>(async (key, factory, expiration, localExpiration, cancellationToken) => await factory(cancellationToken));

        _mockSsoService
            .Setup(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        HttpContext httpContext = context.HttpContext;
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        await _filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull();
        _mockSsoService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnAuthorizationAsync_InvalidTokenValidation_ReturnsUnauthorized()
    {
        // Arrange
        string validToken = CreateValidToken();
        AuthorizationFilterContext context = CreateAuthorizationFilterContext();
        context.HttpContext.Request.Headers.Authorization = $"Bearer {validToken}";

        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<bool>>, TimeSpan?, TimeSpan?, CancellationToken>(async (key, factory, expiration, localExpiration, cancellationToken) => await factory(cancellationToken));

        _mockSsoService
            .Setup(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        HttpContext httpContext = context.HttpContext;
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        await _filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>();
        _mockSsoService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnAuthorizationAsync_TokenInCache_UsesCachedValue()
    {
        // Arrange
        string validToken = CreateValidToken();
        AuthorizationFilterContext context = CreateAuthorizationFilterContext();
        context.HttpContext.Request.Headers.Authorization = $"Bearer {validToken}";

        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Cache hit

        HttpContext httpContext = context.HttpContext;
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        await _filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull();
        _mockSsoService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnAuthorizationAsync_CacheError_FallsBackToSsoService()
    {
        // Arrange
        string validToken = CreateValidToken();
        AuthorizationFilterContext context = CreateAuthorizationFilterContext();
        context.HttpContext.Request.Headers.Authorization = $"Bearer {validToken}";

        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        _mockSsoService
            .Setup(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        HttpContext httpContext = context.HttpContext;
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        await _filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull();
        _mockSsoService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Helper Methods

    private static AuthorizationFilterContext CreateAuthorizationFilterContext()
    {
        DefaultHttpContext httpContext = new();
        ActionDescriptor actionDescriptor = new();
        ActionContext actionContext = new(httpContext, new Microsoft.AspNetCore.Routing.RouteData(), actionDescriptor);
        AuthorizationFilterContext context = new(actionContext, new List<IFilterMetadata>());
        return context;
    }

    private static string CreateValidToken()
    {
        List<Claim> claims = new()
        {
            new Claim("sub", "user123"),
            new Claim("userId", "user123"),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        JwtSecurityToken token = new(
            issuer: "test",
            audience: "test",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: null);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string CreateExpiredToken()
    {
        List<Claim> claims = new()
        {
            new Claim("sub", "user123"),
            new Claim("userId", "user123"),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        JwtSecurityToken token = new(
            issuer: "test",
            audience: "test",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: null);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    #endregion
}
