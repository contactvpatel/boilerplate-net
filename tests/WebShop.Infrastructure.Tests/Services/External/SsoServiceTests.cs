using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using WebShop.Core.Models;
using WebShop.Infrastructure.Services.External;
using WebShop.Util.Models;
using Xunit;

namespace WebShop.Infrastructure.Tests.Services.External;

/// <summary>
/// Unit tests for SsoService.
/// </summary>
[Trait("Category", "Unit")]
public class SsoServiceTests
{
    private readonly Mock<IHttpClientFactory> _mockHttpClientFactory;
    private readonly Mock<IOptions<SsoServiceOptions>> _mockOptions;
    private readonly Mock<ILogger<SsoService>> _mockLogger;
    private readonly Mock<IOptions<HttpResilienceOptions>> _mockResilienceOptions;
    private readonly SsoService _service;

    public SsoServiceTests()
    {
        _mockHttpClientFactory = new Mock<IHttpClientFactory>();
        _mockOptions = new Mock<IOptions<SsoServiceOptions>>();
        _mockLogger = new Mock<ILogger<SsoService>>();
        _mockResilienceOptions = new Mock<IOptions<HttpResilienceOptions>>();

        _mockOptions.Setup(o => o.Value).Returns(new SsoServiceOptions
        {
            Url = "https://sso.example.com",
            Endpoint = new SsoServiceEndpoints
            {
                ValidateToken = "/api/validate",
                RenewToken = "/api/renew",
                Logout = "/api/logout"
            }
        });

        _mockResilienceOptions.Setup(o => o.Value).Returns(new HttpResilienceOptions
        {
            MaxRequestSizeBytes = 1024 * 1024,
            MaxResponseSizeBytes = 10 * 1024 * 1024
        });

        _service = new SsoService(
            _mockHttpClientFactory.Object,
            _mockOptions.Object,
            _mockLogger.Object,
            _mockResilienceOptions.Object);
    }

    #region ValidateTokenAsync Tests

    [Fact]
    public async Task ValidateTokenAsync_ValidToken_ReturnsTrue()
    {
        // Arrange
        const string token = "valid-token";
        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, true);
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://sso.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("SsoService")).Returns(httpClient);

        // Act
        bool result = await _service.ValidateTokenAsync(token);

        // Assert
        result.Should().BeTrue();
        VerifyRequest(mockHandler, HttpMethod.Post, "/api/validate");
    }

    [Fact]
    public async Task ValidateTokenAsync_InvalidToken_ReturnsFalse()
    {
        // Arrange
        const string token = "invalid-token";
        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.Unauthorized, false);
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://sso.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("SsoService")).Returns(httpClient);

        // Act
        bool result = await _service.ValidateTokenAsync(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_NullToken_ReturnsFalse()
    {
        // Act
        bool result = await _service.ValidateTokenAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_EmptyToken_ReturnsFalse()
    {
        // Act
        bool result = await _service.ValidateTokenAsync(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateTokenAsync_WhitespaceToken_ReturnsFalse()
    {
        // Act
        bool result = await _service.ValidateTokenAsync("   ");

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
        SsoAuthResponse expectedResponse = new()
        {
            AccessToken = "new-access-token",
            RefreshToken = "new-refresh-token",
            ExpiresIn = 3600
        };

        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, expectedResponse);
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://sso.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("SsoService")).Returns(httpClient);

        // Act
        SsoAuthResponse? result = await _service.RenewTokenAsync(accessToken, refreshToken);

        // Assert
        result.Should().NotBeNull();
        result!.AccessToken.Should().Be(expectedResponse.AccessToken);
        result.RefreshToken.Should().Be(expectedResponse.RefreshToken);
        VerifyRequest(mockHandler, HttpMethod.Post, "/api/renew");
    }

    [Fact]
    public async Task RenewTokenAsync_NullAccessToken_ReturnsNull()
    {
        // Act
        SsoAuthResponse? result = await _service.RenewTokenAsync(null!, "refresh-token");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task RenewTokenAsync_NullRefreshToken_ReturnsNull()
    {
        // Act
        SsoAuthResponse? result = await _service.RenewTokenAsync("access-token", null!);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region LogoutAsync Tests

    [Fact]
    public async Task LogoutAsync_ValidToken_ReturnsTrue()
    {
        // Arrange
        const string token = "valid-token";
        Mock<HttpMessageHandler> mockHandler = CreateMockHandler(HttpStatusCode.OK, true);
        HttpClient httpClient = new(mockHandler.Object) { BaseAddress = new Uri("https://sso.example.com") };
        _mockHttpClientFactory.Setup(f => f.CreateClient("SsoService")).Returns(httpClient);

        // Act
        bool result = await _service.LogoutAsync(token);

        // Assert
        result.Should().BeTrue();
        VerifyRequest(mockHandler, HttpMethod.Post, "/api/logout");
    }

    [Fact]
    public async Task LogoutAsync_NullToken_ReturnsFalse()
    {
        // Act
        bool result = await _service.LogoutAsync(null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task LogoutAsync_EmptyToken_ReturnsFalse()
    {
        // Act
        bool result = await _service.LogoutAsync(string.Empty);

        // Assert
        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static Mock<HttpMessageHandler> CreateMockHandler<T>(HttpStatusCode statusCode, T response)
    {
        Mock<HttpMessageHandler> mockHandler = new(MockBehavior.Strict);
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = JsonContent.Create(response)
            });
        return mockHandler;
    }

    private static void VerifyRequest(Mock<HttpMessageHandler> mockHandler, HttpMethod method, string endpoint)
    {
        mockHandler
            .Protected()
            .Verify(
                "SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == method &&
                    req.RequestUri!.PathAndQuery.Contains(endpoint)),
                ItExpr.IsAny<CancellationToken>());
    }

    #endregion
}
