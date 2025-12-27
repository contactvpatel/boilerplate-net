using Asp.Versioning;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
using Moq;
using WebShop.Api.Middleware;
using WebShop.Api.Models;
using Xunit;

namespace WebShop.Api.Tests.Middleware;

/// <summary>
/// Unit tests for ApiVersionDeprecationMiddleware.
/// </summary>
[Trait("Category", "Unit")]
public class ApiVersionDeprecationMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly ApiVersionDeprecationOptions _options;
    private readonly ApiVersionDeprecationMiddleware _middleware;

    public ApiVersionDeprecationMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _options = new ApiVersionDeprecationOptions
        {
            DeprecatedVersions = new List<DeprecatedVersion>
            {
                new() { MajorVersion = 1, IsDeprecated = true, DeprecationMessage = "true", SunsetDate = "2025-12-31", SuccessorVersionUrl = "https://api.example.com/v2" }
            }
        };
        Mock<IOptions<ApiVersionDeprecationOptions>> mockOptions = new();
        mockOptions.Setup(o => o.Value).Returns(_options);
        _middleware = new ApiVersionDeprecationMiddleware(_mockNext.Object, mockOptions.Object);
    }

    #region InvokeAsync Tests

    [Fact]
    public async Task InvokeAsync_NonApiPath_DoesNotAddHeaders()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        context.Request.Path = "/health";
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("Deprecation").Should().BeFalse();
        context.Response.Headers.ContainsKey("Sunset").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_ApiPath_NoVersion_DoesNotAddHeaders()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        context.Request.Path = "/api/customers";
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("Deprecation").Should().BeFalse();
    }

    [Fact]
    public async Task InvokeAsync_DeprecatedVersion_AddsDeprecationHeader()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        context.Request.Path = "/api/v1/customers";
        SetApiVersion(context, new ApiVersion(1, 0));
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("Deprecation").Should().BeTrue();
        context.Response.Headers["Deprecation"].ToString().Should().Be("true");
    }

    [Fact]
    public async Task InvokeAsync_DeprecatedVersion_WithSunsetDate_AddsSunsetHeader()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        context.Request.Path = "/api/v1/customers";
        SetApiVersion(context, new ApiVersion(1, 0));
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("Sunset").Should().BeTrue();
        context.Response.Headers["Sunset"].ToString().Should().Be("2025-12-31");
    }

    [Fact]
    public async Task InvokeAsync_DeprecatedVersion_WithSuccessorUrl_AddsLinkHeader()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        context.Request.Path = "/api/v1/customers";
        SetApiVersion(context, new ApiVersion(1, 0));
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("Link").Should().BeTrue();
        context.Response.Headers["Link"].ToString().Should().Contain("successor-version");
        context.Response.Headers["Link"].ToString().Should().Contain("https://api.example.com/v2");
    }

    [Fact]
    public async Task InvokeAsync_NonDeprecatedVersion_DoesNotAddHeaders()
    {
        // Arrange
        _options.DeprecatedVersions[0].IsDeprecated = false;
        DefaultHttpContext context = CreateHttpContext();
        context.Request.Path = "/api/v1/customers";
        SetApiVersion(context, new ApiVersion(1, 0));
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.Headers.ContainsKey("Deprecation").Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static DefaultHttpContext CreateHttpContext()
    {
        DefaultHttpContext context = new();
        context.Request.Path = "/api/v1/test";
        context.Request.Method = "GET";

        // Set up RequestServices to avoid null reference when getting API version
        Mock<IServiceProvider> mockServiceProvider = new();
        context.RequestServices = mockServiceProvider.Object;

        return context;
    }

    private static void SetApiVersion(HttpContext context, ApiVersion version)
    {
        RouteData routeData = new();
        routeData.Values["version"] = version.ToString();
        context.Features.Set<IRoutingFeature>(new RoutingFeature { RouteData = routeData });
        context.Request.RouteValues = routeData.Values;

        // Set IApiVersioningFeature to provide the requested API version
        ApiVersioningFeature feature = new(context)
        {
            RequestedApiVersion = version
        };
        context.Features.Set<IApiVersioningFeature>(feature);

        // Ensure RequestServices is set
        if (context.RequestServices == null)
        {
            Mock<IServiceProvider> mockServiceProvider = new();
            context.RequestServices = mockServiceProvider.Object;
        }
    }

    private class RoutingFeature : IRoutingFeature
    {
        public RouteData? RouteData { get; set; } = new();
    }

    #endregion
}
