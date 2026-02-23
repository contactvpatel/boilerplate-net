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
using WebShop.UnitTests.Common;
using WebShop.Util.Security;
using Xunit;
using ISsoService = WebShop.Business.Services.Interfaces.ISsoService;

namespace WebShop.UnitTests.API.Filters;

/// <summary>
/// Unit tests for JwtTokenAuthenticationFilter.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class JwtTokenAuthenticationFilterTests
{
    private readonly Mock<ISsoService> mockSsoService = new();
    private readonly Mock<IHttpContextAccessor> mockHttpContextAccessor = new();
    private readonly Mock<ICacheService> mockCacheService = new();
    private readonly Mock<ILogger<JwtTokenAuthenticationFilter>> mockLogger = new();
    private readonly JwtTokenAuthenticationFilter filter;

    public JwtTokenAuthenticationFilterTests()
    {
        filter = new JwtTokenAuthenticationFilter(mockSsoService.Object, mockHttpContextAccessor.Object, mockCacheService.Object, mockLogger.Object);
    }

    #region OnAuthorizationAsync Tests

    [Fact]
    public async Task OnAuthorizationAsync_NullContext_DoesNotThrow()
    {
        Func<Task> act = async () => await filter.OnAuthorizationAsync(null!);
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
        await filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull();
        mockSsoService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnAuthorizationAsync_MissingToken_ReturnsUnauthorized()
    {
        // Arrange
        AuthorizationFilterContext context = CreateAuthorizationFilterContext();

        // Act
        await filter.OnAuthorizationAsync(context);

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
        await filter.OnAuthorizationAsync(context);

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
        await filter.OnAuthorizationAsync(context);

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

        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<bool>>, TimeSpan?, TimeSpan?, CancellationToken>(async (key, factory, expiration, localExpiration, cancellationToken) => await factory(cancellationToken));

        mockSsoService
            .Setup(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        HttpContext httpContext = context.HttpContext;
        mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull();
        mockSsoService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnAuthorizationAsync_InvalidTokenValidation_ReturnsUnauthorized()
    {
        // Arrange
        string validToken = CreateValidToken();
        AuthorizationFilterContext context = CreateAuthorizationFilterContext();
        context.HttpContext.Request.Headers.Authorization = $"Bearer {validToken}";

        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<bool>>, TimeSpan?, TimeSpan?, CancellationToken>(async (key, factory, expiration, localExpiration, cancellationToken) => await factory(cancellationToken));

        mockSsoService
            .Setup(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        HttpContext httpContext = context.HttpContext;
        mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeOfType<UnauthorizedObjectResult>();
        mockSsoService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task OnAuthorizationAsync_TokenInCache_UsesCachedValue()
    {
        // Arrange
        string validToken = CreateValidToken();
        AuthorizationFilterContext context = CreateAuthorizationFilterContext();
        context.HttpContext.Request.Headers.Authorization = $"Bearer {validToken}";

        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Cache hit

        HttpContext httpContext = context.HttpContext;
        mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull();
        mockSsoService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OnAuthorizationAsync_CacheError_FallsBackToSsoService()
    {
        // Arrange
        string validToken = CreateValidToken();
        AuthorizationFilterContext context = CreateAuthorizationFilterContext();
        context.HttpContext.Request.Headers.Authorization = $"Bearer {validToken}";

        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        mockSsoService
            .Setup(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        HttpContext httpContext = context.HttpContext;
        mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        await filter.OnAuthorizationAsync(context);

        // Assert
        context.Result.Should().BeNull();
        mockSsoService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
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
