using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using WebShop.Util.Security;
using Xunit;

namespace WebShop.Util.Tests.Security;

/// <summary>
/// Unit tests for JwtTokenHelper.
/// </summary>
[Trait("Category", "Unit")]
public class JwtTokenHelperTests
{
    #region ParseToken Tests

    [Fact]
    public void ParseToken_ValidToken_ReturnsJwtSecurityToken()
    {
        // Arrange
        string token = CreateValidToken();

        // Act
        JwtSecurityToken? result = JwtTokenHelper.ParseToken(token);

        // Assert
        result.Should().NotBeNull();
        result!.Claims.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        const string token = "invalid-token";

        // Act
        JwtSecurityToken? result = JwtTokenHelper.ParseToken(token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseToken_NullToken_ReturnsNull()
    {
        // Act
        JwtSecurityToken? result = JwtTokenHelper.ParseToken(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ParseToken_EmptyToken_ReturnsNull()
    {
        // Act
        JwtSecurityToken? result = JwtTokenHelper.ParseToken(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetClaimValue Tests

    [Fact]
    public void GetClaimValue_ValidClaim_ReturnsClaimValue()
    {
        // Arrange
        JwtSecurityToken token = CreateJwtTokenWithClaims(new Claim("test-claim", "test-value"));

        // Act
        string? result = JwtTokenHelper.GetClaimValue(token, "test-claim");

        // Assert
        result.Should().Be("test-value");
    }

    [Fact]
    public void GetClaimValue_NonExistentClaim_ReturnsNull()
    {
        // Arrange
        JwtSecurityToken token = CreateJwtTokenWithClaims();

        // Act
        string? result = JwtTokenHelper.GetClaimValue(token, "non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetClaimValue_NullToken_ReturnsNull()
    {
        // Act
        string? result = JwtTokenHelper.GetClaimValue(null, "test-claim");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetClaimValue_CaseInsensitive_ReturnsValue()
    {
        // Arrange
        JwtSecurityToken token = CreateJwtTokenWithClaims(new Claim("Test-Claim", "test-value"));

        // Act
        string? result = JwtTokenHelper.GetClaimValue(token, "test-claim");

        // Assert
        result.Should().Be("test-value");
    }

    #endregion

    #region GetUserId Tests

    [Fact]
    public void GetUserId_TokenWithUserId_ReturnsUserId()
    {
        // Arrange
        JwtSecurityToken token = CreateJwtTokenWithClaims(new Claim(JwtTokenHelper.ClaimTypeUserId, "user-123"));

        // Act
        string? result = JwtTokenHelper.GetUserId(token);

        // Assert
        result.Should().Be("user-123");
    }

    [Fact]
    public void GetUserId_TokenWithoutUserId_ReturnsNull()
    {
        // Arrange
        JwtSecurityToken token = CreateJwtTokenWithClaims();

        // Act
        string? result = JwtTokenHelper.GetUserId(token);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetTokenExpiration Tests

    [Fact]
    public void GetTokenExpiration_TokenWithExpiration_ReturnsExpiration()
    {
        // Arrange
        DateTimeOffset expiration = DateTimeOffset.UtcNow.AddHours(1);
        JwtSecurityToken token = CreateJwtTokenWithClaims(
            new Claim(JwtTokenHelper.ClaimTypeExpiration, expiration.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

        // Act
        DateTimeOffset? result = JwtTokenHelper.GetTokenExpiration(token);

        // Assert
        result.Should().NotBeNull();
        result!.Value.Should().BeCloseTo(expiration.ToLocalTime(), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetTokenExpiration_TokenWithoutExpiration_ReturnsNull()
    {
        // Arrange
        JwtSecurityToken token = CreateJwtTokenWithClaims();

        // Act
        DateTimeOffset? result = JwtTokenHelper.GetTokenExpiration(token);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetTokenExpiration_InvalidExpirationFormat_ReturnsNull()
    {
        // Arrange
        JwtSecurityToken token = CreateJwtTokenWithClaims(
            new Claim(JwtTokenHelper.ClaimTypeExpiration, "invalid", ClaimValueTypes.String));

        // Act
        DateTimeOffset? result = JwtTokenHelper.GetTokenExpiration(token);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region IsTokenExpired Tests

    [Fact]
    public void IsTokenExpired_ExpiredToken_ReturnsTrue()
    {
        // Arrange
        DateTimeOffset expiration = DateTimeOffset.UtcNow.AddHours(-1);
        JwtSecurityToken token = CreateJwtTokenWithClaims(
            new Claim(JwtTokenHelper.ClaimTypeExpiration, expiration.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

        // Act
        bool result = JwtTokenHelper.IsTokenExpired(token);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void IsTokenExpired_ValidToken_ReturnsFalse()
    {
        // Arrange
        DateTimeOffset expiration = DateTimeOffset.UtcNow.AddHours(1);
        JwtSecurityToken token = CreateJwtTokenWithClaims(
            new Claim(JwtTokenHelper.ClaimTypeExpiration, expiration.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

        // Act
        bool result = JwtTokenHelper.IsTokenExpired(token);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsTokenExpired_TokenWithoutExpiration_ReturnsTrue()
    {
        // Arrange
        JwtSecurityToken token = CreateJwtTokenWithClaims();

        // Act
        bool result = JwtTokenHelper.IsTokenExpired(token);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region ExtractBearerToken Tests

    [Fact]
    public void ExtractBearerToken_ValidBearerToken_ReturnsToken()
    {
        // Arrange
        const string authorizationHeader = "Bearer test-token-123";

        // Act
        string? result = JwtTokenHelper.ExtractBearerToken(authorizationHeader);

        // Assert
        result.Should().Be("test-token-123");
    }

    [Fact]
    public void ExtractBearerToken_CaseInsensitive_ReturnsToken()
    {
        // Arrange
        const string authorizationHeader = "bearer test-token-123";

        // Act
        string? result = JwtTokenHelper.ExtractBearerToken(authorizationHeader);

        // Assert
        result.Should().Be("test-token-123");
    }

    [Fact]
    public void ExtractBearerToken_NoBearerPrefix_ReturnsNull()
    {
        // Arrange
        const string authorizationHeader = "test-token-123";

        // Act
        string? result = JwtTokenHelper.ExtractBearerToken(authorizationHeader);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractBearerToken_NullHeader_ReturnsNull()
    {
        // Act
        string? result = JwtTokenHelper.ExtractBearerToken(null);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractBearerToken_EmptyToken_ReturnsNull()
    {
        // Arrange
        const string authorizationHeader = "Bearer ";

        // Act
        string? result = JwtTokenHelper.ExtractBearerToken(authorizationHeader);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GenerateCacheKey Tests

    [Fact]
    public void GenerateCacheKey_ValidToken_ReturnsCacheKey()
    {
        // Arrange
        const string token = "test-token-123";

        // Act
        string result = JwtTokenHelper.GenerateCacheKey(token);

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().StartWith(JwtTokenHelper.JwtTokenCacheKeyPrefix);
        result.Length.Should().BeGreaterThan(JwtTokenHelper.JwtTokenCacheKeyPrefix.Length);
    }

    [Fact]
    public void GenerateCacheKey_SameToken_ReturnsSameKey()
    {
        // Arrange
        const string token = "test-token-123";

        // Act
        string result1 = JwtTokenHelper.GenerateCacheKey(token);
        string result2 = JwtTokenHelper.GenerateCacheKey(token);

        // Assert
        result1.Should().Be(result2);
    }

    [Fact]
    public void GenerateCacheKey_DifferentTokens_ReturnsDifferentKeys()
    {
        // Arrange
        const string token1 = "test-token-123";
        const string token2 = "test-token-456";

        // Act
        string result1 = JwtTokenHelper.GenerateCacheKey(token1);
        string result2 = JwtTokenHelper.GenerateCacheKey(token2);

        // Assert
        result1.Should().NotBe(result2);
    }

    [Fact]
    public void GenerateCacheKey_NullToken_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            JwtTokenHelper.GenerateCacheKey(null!));
    }

    [Fact]
    public void GenerateCacheKey_EmptyToken_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            JwtTokenHelper.GenerateCacheKey(string.Empty));
    }

    #endregion

    #region Helper Methods

    private static string CreateValidToken()
    {
        List<Claim> claims = new()
        {
            new Claim("sub", "user123"),
            new Claim(JwtTokenHelper.ClaimTypeUserId, "user123"),
            new Claim(JwtTokenHelper.ClaimTypeExpiration, DateTimeOffset.UtcNow.AddHours(1).ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        JwtSecurityToken token = new(
            issuer: "test",
            audience: "test",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: null);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static JwtSecurityToken CreateJwtTokenWithClaims(params Claim[] claims)
    {
        List<Claim> allClaims = new(claims)
        {
            new Claim("sub", "user123")
        };

        return new JwtSecurityToken(
            issuer: "test",
            audience: "test",
            claims: allClaims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: null);
    }

    #endregion
}
