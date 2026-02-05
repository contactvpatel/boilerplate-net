using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Business.DTOs;
using WebShop.Business.Services;
using WebShop.Core.Interfaces.Base;
using Xunit;

namespace WebShop.Business.Tests.Services;

/// <summary>
/// Unit tests for SsoService.
/// </summary>
[Trait("Category", "Unit")]
public class SsoServiceTests
{
    private readonly Mock<Core.Interfaces.Services.ISsoService> _mockCoreService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<SsoService>> _mockLogger;
    private readonly SsoService _service;

    public SsoServiceTests()
    {
        _mockCoreService = new Mock<Core.Interfaces.Services.ISsoService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<SsoService>>();
        _service = new SsoService(_mockCoreService.Object, _mockCacheService.Object, _mockLogger.Object);
    }

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsTrue()
    {
        // Arrange
        const string token = "valid-token";
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<bool>>, TimeSpan?, TimeSpan?, CancellationToken>(
                async (key, factory, exp, localExp, cancellationToken) => await factory(cancellationToken));

        _mockCoreService
            .Setup(s => s.ValidateTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        bool result = await _service.ValidateTokenAsync(token);

        // Assert
        result.Should().BeTrue();
        _mockCoreService.Verify(s => s.ValidateTokenAsync(token, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsFalse()
    {
        // Arrange
        const string token = "invalid-token";
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<bool>>, TimeSpan?, TimeSpan?, CancellationToken>(
                async (key, factory, exp, localExp, cancellationToken) => await factory(cancellationToken));

        _mockCoreService
            .Setup(s => s.ValidateTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockCacheService
            .Setup(c => c.SetAsync(
                It.IsAny<string>(),
                false,
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        bool result = await _service.ValidateTokenAsync(token);

        // Assert
        result.Should().BeFalse();
        _mockCoreService.Verify(s => s.ValidateTokenAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            false,
            It.IsAny<TimeSpan?>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_DoesNotCallSetAsync()
    {
        // Arrange
        const string token = "valid-token";
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<bool>>, TimeSpan?, TimeSpan?, CancellationToken>(
                async (key, factory, exp, localExp, cancellationToken) => await factory(cancellationToken));

        _mockCoreService
            .Setup(s => s.ValidateTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        bool result = await _service.ValidateTokenAsync(token);

        // Assert
        result.Should().BeTrue();
        _mockCoreService.Verify(s => s.ValidateTokenAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        // Verify SetAsync is NOT called for valid tokens (else branch)
        _mockCacheService.Verify(c => c.SetAsync(
            It.IsAny<string>(),
            It.IsAny<bool>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<TimeSpan?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateTokenAsync_NullOrEmptyToken_ReturnsFalse()
    {
        // Arrange
        const string? token = null;

        // Act
        bool result = await _service.ValidateTokenAsync(token!);

        // Assert
        result.Should().BeFalse();
        _mockCoreService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateTokenAsync_WhitespaceToken_ReturnsFalse()
    {
        // Arrange
        const string token = "   ";

        // Act
        bool result = await _service.ValidateTokenAsync(token);

        // Assert
        result.Should().BeFalse();
        _mockCoreService.Verify(s => s.ValidateTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ValidateTokenAsync_ExceptionThrown_ReturnsFalse()
    {
        // Arrange
        const string token = "token";
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        bool result = await _service.ValidateTokenAsync(token);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region RenewTokenAsync Tests

    [Fact]
    public async Task RenewTokenAsync_ValidTokens_ReturnsAuthResponse()
    {
        // Arrange
        const string accessToken = "access-token";
        const string refreshToken = "refresh-token";
        Core.Models.SsoAuthResponse coreResponse = new Core.Models.SsoAuthResponse
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            ExpiresIn = 3600
        };

        _mockCoreService
            .Setup(s => s.RenewTokenAsync(accessToken, refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coreResponse);

        // Act
        SsoAuthResponse? result = await _service.RenewTokenAsync(accessToken, refreshToken);

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be("new-access-token");
        result.RefreshToken.Should().Be("new-refresh-token");
        _mockCoreService.Verify(s => s.RenewTokenAsync(accessToken, refreshToken, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RenewTokenAsync_InvalidTokens_ReturnsNull()
    {
        // Arrange
        const string accessToken = "invalid-access-token";
        const string refreshToken = "invalid-refresh-token";

        _mockCoreService
            .Setup(s => s.RenewTokenAsync(accessToken, refreshToken, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Core.Models.SsoAuthResponse?)null);

        // Act
        SsoAuthResponse? result = await _service.RenewTokenAsync(accessToken, refreshToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RenewTokenAsync_NullAccessToken_ThrowsArgumentException()
    {
        // Arrange
        const string? accessToken = null;
        const string refreshToken = "refresh-token";

        // Act
        Func<Task> act = async () => await _service.RenewTokenAsync(accessToken!, refreshToken);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task RenewTokenAsync_NullRefreshToken_ThrowsArgumentException()
    {
        // Arrange
        const string accessToken = "access-token";
        const string? refreshToken = null;

        // Act
        Func<Task> act = async () => await _service.RenewTokenAsync(accessToken, refreshToken!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_ValidToken_ReturnsTrue()
    {
        // Arrange
        const string token = "valid-token";

        _mockCoreService
            .Setup(s => s.LogoutAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockCacheService
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        bool result = await _service.LogoutAsync(token);

        // Assert
        result.Should().BeTrue();
        _mockCoreService.Verify(s => s.LogoutAsync(token, It.IsAny<CancellationToken>()), Times.Once);
        _mockCacheService.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_InvalidToken_ReturnsFalse()
    {
        // Arrange
        const string token = "invalid-token";

        _mockCoreService
            .Setup(s => s.LogoutAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _mockCacheService
            .Setup(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        bool result = await _service.LogoutAsync(token);

        // Assert
        result.Should().BeFalse();
        _mockCacheService.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_NullToken_DoesNotRemoveFromCache()
    {
        // Arrange
        const string? token = null;

        _mockCoreService
            .Setup(s => s.LogoutAsync(token!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.LogoutAsync(token!);

        // Assert
        result.Should().BeFalse();
        _mockCacheService.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutAsync_EmptyToken_DoesNotRemoveFromCache()
    {
        // Arrange
        const string token = "";

        _mockCoreService
            .Setup(s => s.LogoutAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.LogoutAsync(token);

        // Assert
        result.Should().BeFalse();
        _mockCacheService.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion
}
