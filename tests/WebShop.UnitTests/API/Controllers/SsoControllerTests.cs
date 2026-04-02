using System.IdentityModel.Tokens.Jwt;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using WebShop.Api.Controllers;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Core.Interfaces.Base;
using WebShop.UnitTests.Common;
using Xunit;
using ISsoService = WebShop.Business.Services.Interfaces.ISsoService;

namespace WebShop.UnitTests.API.Controllers;

/// <summary>
/// Unit tests for SsoController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class SsoControllerTests
{
    private readonly Mock<ISsoService> mockSsoService = new();
    private readonly Mock<IUserContext> mockUserContext = new();
    private readonly Mock<ICacheService> mockCacheService = new();
    private readonly SsoController controller;

    public SsoControllerTests()
    {
        controller = new SsoController(mockSsoService.Object, mockUserContext.Object, mockCacheService.Object);
    }

    #region RenewToken Tests

    [Fact]
    public async Task RenewToken_ValidTokens_ReturnsOk()
    {
        // Arrange
        const string accessToken = "valid-access-token";
        const string refreshToken = "valid-refresh-token";
        SsoRenewTokenRequest request = new SsoRenewTokenRequest { RefreshToken = refreshToken };
        SsoAuthResponse authResponse = new SsoAuthResponse
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            ExpiresIn = 3600
        };

        mockUserContext.Setup(u => u.GetToken()).Returns(accessToken);
        mockSsoService
            .Setup(s => s.RenewTokenAsync(accessToken, refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(authResponse);

        // Act
        ActionResult<Response<SsoAuthResponse>> result = await controller.RenewToken(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<SsoAuthResponse>? response = okResult!.Value as Response<SsoAuthResponse>;
        response!.Data.Should().NotBeNull();
        response.Data!.AccessToken.Should().Be("new-access-token");
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task RenewToken_MissingAccessToken_ReturnsUnauthorized()
    {
        // Arrange
        const string refreshToken = "valid-refresh-token";
        SsoRenewTokenRequest request = new SsoRenewTokenRequest { RefreshToken = refreshToken };

        mockUserContext.Setup(u => u.GetToken()).Returns((string?)null);

        // Act
        ActionResult<Response<SsoAuthResponse>> result = await controller.RenewToken(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        UnauthorizedObjectResult? unauthorizedResult = result.Result as UnauthorizedObjectResult;
        Response<SsoAuthResponse>? response = unauthorizedResult!.Value as Response<SsoAuthResponse>;
        response!.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task RenewToken_MissingRefreshToken_ReturnsUnauthorized()
    {
        // Arrange
        const string accessToken = "valid-access-token";
        SsoRenewTokenRequest request = new SsoRenewTokenRequest { RefreshToken = string.Empty };

        mockUserContext.Setup(u => u.GetToken()).Returns(accessToken);

        // Act
        ActionResult<Response<SsoAuthResponse>> result = await controller.RenewToken(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        UnauthorizedObjectResult? unauthorizedResult = result.Result as UnauthorizedObjectResult;
        Response<SsoAuthResponse>? response = unauthorizedResult!.Value as Response<SsoAuthResponse>;
        response!.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task RenewToken_InvalidTokens_ReturnsUnauthorized()
    {
        // Arrange
        const string accessToken = "invalid-access-token";
        const string refreshToken = "invalid-refresh-token";
        SsoRenewTokenRequest request = new SsoRenewTokenRequest { RefreshToken = refreshToken };

        mockUserContext.Setup(u => u.GetToken()).Returns(accessToken);
        mockSsoService
            .Setup(s => s.RenewTokenAsync(accessToken, refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SsoAuthResponse?)null);

        // Act
        ActionResult<Response<SsoAuthResponse>> result = await controller.RenewToken(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        UnauthorizedObjectResult? unauthorizedResult = result.Result as UnauthorizedObjectResult;
        Response<SsoAuthResponse>? response = unauthorizedResult!.Value as Response<SsoAuthResponse>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion

    #region Logout Tests

    [Fact]
    public async Task Logout_ValidToken_ReturnsOk()
    {
        // Arrange
        const string token = "valid-token";
        const string userId = "user-123";

        mockUserContext.Setup(u => u.GetToken()).Returns(token);
        mockUserContext.Setup(u => u.GetUserId()).Returns(userId);
        mockSsoService
            .Setup(s => s.LogoutAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        mockCacheService
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Mock JwtTokenHelper static methods by using reflection or creating a wrapper
        // For now, we'll test the flow assuming the token is not expired

        // Act
        ActionResult<Response<bool>> result = await controller.Logout(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<bool>? response = okResult!.Value as Response<bool>;
        response!.Data.Should().BeTrue();
        response.Succeeded.Should().BeTrue();
        mockSsoService.Verify(s => s.LogoutAsync(token, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_ExpiredToken_SkipsSsoCallAndClearsCache()
    {
        // Arrange
        string expiredToken = CreateExpiredToken();
        const string userId = "user-123";

        mockUserContext.Setup(u => u.GetToken()).Returns(expiredToken);
        mockUserContext.Setup(u => u.GetUserId()).Returns(userId);
        mockCacheService
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // When token is expired, JwtTokenHelper.IsTokenExpired returns true
        // and SSO API call is skipped, but cache is still cleared

        // Act
        ActionResult<Response<bool>> result = await controller.Logout(CancellationToken.None);

        // Assert
        // The controller should still return Ok if cache clearing succeeds
        result.Result.Should().BeOfType<OkObjectResult>();
        mockSsoService.Verify(s => s.LogoutAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        mockCacheService.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    private static string CreateExpiredToken()
    {
        List<System.Security.Claims.Claim> claims = new()
        {
            new System.Security.Claims.Claim("sub", "user123"),
            new System.Security.Claims.Claim("userId", "user123"),
            new System.Security.Claims.Claim(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddHours(-1).ToUnixTimeSeconds().ToString(), System.Security.Claims.ClaimValueTypes.Integer64)
        };

        JwtSecurityToken token = new(
            issuer: "test",
            audience: "test",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: null);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task Logout_MissingToken_ReturnsUnauthorized()
    {
        // Arrange
        mockUserContext.Setup(u => u.GetToken()).Returns((string?)null);

        // Act
        ActionResult<Response<bool>> result = await controller.Logout(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        UnauthorizedObjectResult? unauthorizedResult = result.Result as UnauthorizedObjectResult;
        Response<bool>? response = unauthorizedResult!.Value as Response<bool>;
        response!.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_LogoutFails_ReturnsUnauthorized()
    {
        // Arrange
        const string token = "valid-token";
        const string userId = "user-123";

        mockUserContext.Setup(u => u.GetToken()).Returns(token);
        mockUserContext.Setup(u => u.GetUserId()).Returns(userId);
        mockSsoService
            .Setup(s => s.LogoutAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        ActionResult<Response<bool>> result = await controller.Logout(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        UnauthorizedObjectResult? unauthorizedResult = result.Result as UnauthorizedObjectResult;
        Response<bool>? response = unauthorizedResult!.Value as Response<bool>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion
}
