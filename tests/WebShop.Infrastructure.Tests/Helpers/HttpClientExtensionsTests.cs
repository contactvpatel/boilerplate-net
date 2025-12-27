using System.Net.Http.Headers;
using FluentAssertions;
using WebShop.Infrastructure.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Helpers;

/// <summary>
/// Unit tests for HttpClientExtensions.
/// </summary>
[Trait("Category", "Unit")]
public class HttpClientExtensionsTests
{
    #region SetBearerToken Tests

    [Fact]
    public void SetBearerToken_ValidToken_SetsAuthorizationHeader()
    {
        // Arrange
        HttpClient httpClient = new();
        const string token = "test-token";

        // Act
        HttpClient result = httpClient.SetBearerToken(token);

        // Assert
        result.Should().BeSameAs(httpClient);
        httpClient.DefaultRequestHeaders.Authorization.Should().NotBeNull();
        httpClient.DefaultRequestHeaders.Authorization!.Scheme.Should().Be("Bearer");
        httpClient.DefaultRequestHeaders.Authorization.Parameter.Should().Be(token);
    }

    [Fact]
    public void SetBearerToken_NullToken_DoesNotSetHeader()
    {
        // Arrange
        HttpClient httpClient = new();

        // Act
        HttpClient result = httpClient.SetBearerToken(null!);

        // Assert
        result.Should().BeSameAs(httpClient);
        httpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    [Fact]
    public void SetBearerToken_EmptyToken_DoesNotSetHeader()
    {
        // Arrange
        HttpClient httpClient = new();

        // Act
        HttpClient result = httpClient.SetBearerToken(string.Empty);

        // Assert
        result.Should().BeSameAs(httpClient);
        httpClient.DefaultRequestHeaders.Authorization.Should().BeNull();
    }

    #endregion

    #region AddHeader Tests

    [Fact]
    public void AddHeader_ValidHeader_AddsHeader()
    {
        // Arrange
        HttpClient httpClient = new();
        const string headerName = "X-Custom-Header";
        const string headerValue = "test-value";

        // Act
        HttpClient result = httpClient.AddHeader(headerName, headerValue);

        // Assert
        result.Should().BeSameAs(httpClient);
        httpClient.DefaultRequestHeaders.Contains(headerName).Should().BeTrue();
        httpClient.DefaultRequestHeaders.GetValues(headerName).Should().Contain(headerValue);
    }

    [Fact]
    public void AddHeader_HeaderWithCrlf_ThrowsArgumentException()
    {
        // Arrange
        HttpClient httpClient = new();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            httpClient.AddHeader("X-Header\r\n", "value"));
    }

    [Fact]
    public void AddHeader_ValueWithCrlf_ThrowsArgumentException()
    {
        // Arrange
        HttpClient httpClient = new();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            httpClient.AddHeader("X-Header", "value\r\n"));
    }

    [Fact]
    public void AddHeader_HeaderWithControlCharacter_ThrowsArgumentException()
    {
        // Arrange
        HttpClient httpClient = new();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            httpClient.AddHeader("X-Header\t", "value"));
    }

    #endregion

    #region AddHeaders Tests

    [Fact]
    public void AddHeaders_ValidHeaders_AddsAllHeaders()
    {
        // Arrange
        HttpClient httpClient = new();
        Dictionary<string, string> headers = new()
        {
            { "X-Header1", "value1" },
            { "X-Header2", "value2" }
        };

        // Act
        HttpClient result = httpClient.AddHeaders(headers);

        // Assert
        result.Should().BeSameAs(httpClient);
        httpClient.DefaultRequestHeaders.Contains("X-Header1").Should().BeTrue();
        httpClient.DefaultRequestHeaders.Contains("X-Header2").Should().BeTrue();
    }

    [Fact]
    public void AddHeaders_NullDictionary_DoesNotThrow()
    {
        // Arrange
        HttpClient httpClient = new();

        // Act
        HttpClient result = httpClient.AddHeaders(null!);

        // Assert
        result.Should().BeSameAs(httpClient);
    }

    [Fact]
    public void AddHeaders_EmptyDictionary_DoesNotThrow()
    {
        // Arrange
        HttpClient httpClient = new();

        // Act
        HttpClient result = httpClient.AddHeaders(new Dictionary<string, string>());

        // Assert
        result.Should().BeSameAs(httpClient);
    }

    #endregion

    #region HttpRequestMessage SetBearerToken Tests

    [Fact]
    public void SetBearerToken_HttpRequestMessage_ValidToken_SetsAuthorizationHeader()
    {
        // Arrange
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com");
        const string token = "test-token";

        // Act
        HttpRequestMessage result = request.SetBearerToken(token);

        // Assert
        result.Should().BeSameAs(request);
        request.Headers.Authorization.Should().NotBeNull();
        request.Headers.Authorization!.Scheme.Should().Be("Bearer");
        request.Headers.Authorization.Parameter.Should().Be(token);
    }

    [Fact]
    public void SetBearerToken_HttpRequestMessage_NullToken_DoesNotSetHeader()
    {
        // Arrange
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com");

        // Act
        HttpRequestMessage result = request.SetBearerToken(null!);

        // Assert
        result.Should().BeSameAs(request);
        request.Headers.Authorization.Should().BeNull();
    }

    #endregion

    #region HttpRequestMessage AddHeader Tests

    [Fact]
    public void AddHeader_HttpRequestMessage_ValidHeader_AddsHeader()
    {
        // Arrange
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com");
        const string headerName = "X-Custom-Header";
        const string headerValue = "test-value";

        // Act
        HttpRequestMessage result = request.AddHeader(headerName, headerValue);

        // Assert
        result.Should().BeSameAs(request);
        request.Headers.Contains(headerName).Should().BeTrue();
        request.Headers.GetValues(headerName).Should().Contain(headerValue);
    }

    [Fact]
    public void AddHeader_HttpRequestMessage_HeaderWithCrlf_ThrowsArgumentException()
    {
        // Arrange
        HttpRequestMessage request = new(HttpMethod.Get, "https://example.com");

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            request.AddHeader("X-Header\r\n", "value"));
    }

    #endregion

    #region URL Helper Tests

    [Fact]
    public void EnsureTrailingSlash_UrlWithoutSlash_AddsSlash()
    {
        // Arrange
        const string url = "https://example.com/api";

        // Act
        string result = url.EnsureTrailingSlash();

        // Assert
        result.Should().Be("https://example.com/api/");
    }

    [Fact]
    public void EnsureTrailingSlash_UrlWithSlash_KeepsSlash()
    {
        // Arrange
        const string url = "https://example.com/api/";

        // Act
        string result = url.EnsureTrailingSlash();

        // Assert
        result.Should().Be("https://example.com/api/");
    }

    [Fact]
    public void RemoveLeadingSlash_PathWithSlash_RemovesSlash()
    {
        // Arrange
        const string path = "/api/test";

        // Act
        string result = path.RemoveLeadingSlash();

        // Assert
        result.Should().Be("api/test");
    }

    [Fact]
    public void CombineUrl_BaseUrlAndEndpoint_CombinesCorrectly()
    {
        // Arrange
        const string baseUrl = "https://example.com/api";
        const string endpoint = "test";

        // Act
        string result = baseUrl.CombineUrl(endpoint);

        // Assert
        result.Should().Be("https://example.com/api/test");
    }

    [Fact]
    public void CombineUrl_BaseUrlWithSlash_HandlesCorrectly()
    {
        // Arrange
        const string baseUrl = "https://example.com/api/";
        const string endpoint = "/test";

        // Act
        string result = baseUrl.CombineUrl(endpoint);

        // Assert
        result.Should().Be("https://example.com/api/test");
    }

    #endregion
}
