using System;
using FluentAssertions;
using WebShop.Infrastructure.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Helpers;

/// <summary>
/// Unit tests for UrlValidator.
/// </summary>
[Trait("Category", "Unit")]
public class UrlValidatorTests
{
    #region IsValidExternalUrl Tests

    [Fact]
    public void IsValidExternalUrl_ValidHttpsUrl_ReturnsTrue()
    {
        // Arrange
        const string url = "https://api.example.com";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeTrue();
        uri.Should().NotBeNull();
        uri!.Scheme.Should().Be("https");
    }

    [Fact]
    public void IsValidExternalUrl_HttpUrl_ReturnsFalse()
    {
        // Arrange
        const string url = "http://api.example.com";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void IsValidExternalUrl_Localhost_ReturnsFalse()
    {
        // Arrange
        const string url = "https://localhost";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void IsValidExternalUrl_127_0_0_1_ReturnsFalse()
    {
        // Arrange
        const string url = "https://127.0.0.1";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void IsValidExternalUrl_PrivateIpRange_ReturnsFalse()
    {
        // Arrange
        const string url = "https://192.168.1.1";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void IsValidExternalUrl_10_0_0_1_ReturnsFalse()
    {
        // Arrange
        const string url = "https://10.0.0.1";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void IsValidExternalUrl_172_16_0_1_ReturnsFalse()
    {
        // Arrange
        const string url = "https://172.16.0.1";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void IsValidExternalUrl_169_254_0_1_ReturnsFalse()
    {
        // Arrange
        const string url = "https://169.254.0.1";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void IsValidExternalUrl_InvalidUrl_ReturnsFalse()
    {
        // Arrange
        const string url = "not-a-valid-url";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void IsValidExternalUrl_NullUrl_ReturnsFalse()
    {
        // Act
        bool result = UrlValidator.IsValidExternalUrl(null, out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void IsValidExternalUrl_EmptyUrl_ReturnsFalse()
    {
        // Act
        bool result = UrlValidator.IsValidExternalUrl(string.Empty, out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void IsValidExternalUrl_ValidPublicIp_ReturnsTrue()
    {
        // Arrange
        const string url = "https://8.8.8.8";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeTrue();
        uri.Should().NotBeNull();
    }

    #endregion
}
