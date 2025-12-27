using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using WebShop.Infrastructure.Services.Internal;
using Xunit;

namespace WebShop.Infrastructure.Tests.Services.Internal;

/// <summary>
/// Unit tests for UserContext.
/// </summary>
[Trait("Category", "Unit")]
public class UserContextTests
{
    private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
    private readonly UserContext _userContext;

    public UserContextTests()
    {
        _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
        _userContext = new UserContext(_mockHttpContextAccessor.Object);
    }

    #region GetUserId Tests

    [Fact]
    public void GetUserId_WithUserIdInContext_ReturnsUserId()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        httpContext.Items["UserId"] = "user123";
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        string? result = _userContext.GetUserId();

        // Assert
        result.Should().Be("user123");
    }

    [Fact]
    public void GetUserId_NoUserIdInContext_ReturnsNull()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        string? result = _userContext.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetUserId_NullHttpContext_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        // Act
        string? result = _userContext.GetUserId();

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetToken Tests

    [Fact]
    public void GetToken_WithTokenInContext_ReturnsToken()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        httpContext.Items["UserToken"] = "token123";
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        string? result = _userContext.GetToken();

        // Assert
        result.Should().Be("token123");
    }

    [Fact]
    public void GetToken_NoTokenInContext_ReturnsNull()
    {
        // Arrange
        DefaultHttpContext httpContext = new();
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns(httpContext);

        // Act
        string? result = _userContext.GetToken();

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void GetToken_NullHttpContext_ReturnsNull()
    {
        // Arrange
        _mockHttpContextAccessor.Setup(h => h.HttpContext).Returns((HttpContext?)null);

        // Act
        string? result = _userContext.GetToken();

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
