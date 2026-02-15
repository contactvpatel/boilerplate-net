using System;
using FluentAssertions;
using WebShop.Infrastructure.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Helpers;

/// <summary>
/// Unit tests for UrlValidator.
/// </summary>
[Trait("Category", TestCategories.Unit)]
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

    [Fact]
    public void IsValidExternalUrl_172_31_255_255_PrivateRange_ReturnsFalse()
    {
        // Arrange - 172.31.x.x is in private range (172.16.0.0 - 172.31.255.255)
        const string url = "https://172.31.255.255";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    [Fact]
    public void IsValidExternalUrl_172_15_0_1_OutsidePrivateRange_ReturnsTrue()
    {
        // Arrange - 172.15.x.x is outside 172.16-172.31 range
        const string url = "https://172.15.0.1";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeTrue();
        uri.Should().NotBeNull();
    }

    [Fact]
    public void IsValidExternalUrl_172_32_0_1_OutsidePrivateRange_ReturnsTrue()
    {
        // Arrange - 172.32.x.x is outside 172.16-172.31 range
        const string url = "https://172.32.0.1";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert
        result.Should().BeTrue();
        uri.Should().NotBeNull();
    }

    [Fact]
    public void IsValidExternalUrl_172HostnameNotIp_ReturnsTrue()
    {
        // Arrange - host starting with "172." but not a valid IP (e.g. hostname) - IsPrivate172Range returns false for non-parseable
        const string url = "https://172.example.com";

        // Act
        bool result = UrlValidator.IsValidExternalUrl(url, out Uri? uri);

        // Assert - "172.example.com" doesn't parse as IP, so IsPrivate172Range returns false, and we don't block
        result.Should().BeTrue();
        uri.Should().NotBeNull();
    }

    [Fact]
    public void IsValidExternalUrl_WhitespaceUrl_ReturnsFalse()
    {
        // Act
        bool result = UrlValidator.IsValidExternalUrl("   ", out Uri? uri);

        // Assert
        result.Should().BeFalse();
        uri.Should().BeNull();
    }

    #endregion
}
