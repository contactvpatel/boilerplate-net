using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using WebShop.Util.Security;
using Xunit;

namespace WebShop.Infrastructure.Tests.Security;

/// <summary>
/// Unit tests for JwtTokenHelper - additional branch coverage.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class JwtTokenHelperTests
{
    #region GetCacheExpiration Tests

    [Fact]
    public void GetCacheExpiration_TokenWithMoreThanOneSecondRemaining_ReturnsTimeSpan()
    {
        // Arrange
        DateTimeOffset expiration = DateTimeOffset.UtcNow.AddSeconds(60);
        JwtSecurityToken token = CreateJwtTokenWithClaims(
            new Claim(JwtTokenHelper.ClaimTypeExpiration, expiration.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64));

        // Act
        TimeSpan? result = JwtTokenHelper.GetCacheExpiration(token);

        // Assert
        result.Should().NotBeNull();
        result!.Value.TotalSeconds.Should().BeGreaterThan(50);
    }

    [Fact]
    public void GetCacheExpiration_NullToken_ReturnsNull()
    {
        // Act
        TimeSpan? result = JwtTokenHelper.GetCacheExpiration((JwtSecurityToken?)null);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetClaimValue Tests

    [Fact]
    public void GetClaimValue_EmptyClaimType_ReturnsNull()
    {
        // Arrange
        JwtSecurityToken token = CreateJwtTokenWithClaims(new Claim("test", "value"));

        // Act
        string? result = JwtTokenHelper.GetClaimValue(token, "");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetClaimValue_WhitespaceClaimType_ReturnsNull()
    {
        // Arrange
        JwtSecurityToken token = CreateJwtTokenWithClaims(new Claim("test", "value"));

        // Act
        string? result = JwtTokenHelper.GetClaimValue(token, "   ");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetTokenExpiration Tests

    #endregion

    #region ExtractBearerToken Tests

    [Fact]
    public void ExtractBearerToken_BearerWithWhitespaceOnly_ReturnsNull()
    {
        // Arrange
        const string authorizationHeader = "Bearer   ";

        // Act
        string? result = JwtTokenHelper.ExtractBearerToken(authorizationHeader);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractBearerToken_EmptyString_ReturnsNull()
    {
        // Act
        string? result = JwtTokenHelper.ExtractBearerToken(string.Empty);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void ExtractBearerToken_WhitespaceOnly_ReturnsNull()
    {
        // Act
        string? result = JwtTokenHelper.ExtractBearerToken("   ");

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Helper Methods

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
