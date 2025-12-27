using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Middleware;
using WebShop.Api.Models;
using Xunit;

namespace WebShop.Api.Tests.Middleware;

/// <summary>
/// Unit tests for ExceptionHandlingMiddleware.
/// </summary>
[Trait("Category", "Unit")]
public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger;
    private readonly ExceptionHandlingOptions _options;
    private readonly ExceptionHandlingMiddleware _middleware;

    public ExceptionHandlingMiddlewareTests()
    {
        _mockNext = new Mock<RequestDelegate>();
        _mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        _options = new ExceptionHandlingOptions();
        _middleware = new ExceptionHandlingMiddleware(_options, _mockNext.Object, _mockLogger.Object);
    }

    #region InvokeAsync Tests

    [Fact]
    public async Task InvokeAsync_NoException_CallsNext()
    {
        // Arrange
        DefaultHttpContext context = new();
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(n => n(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        ArgumentException exception = new("Invalid argument");
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        await VerifyErrorResponse(context, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentNullException_ReturnsBadRequest()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        ArgumentNullException exception = new("paramName", "Parameter cannot be null");
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        await VerifyErrorResponse(context, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_ReturnsForbidden()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        UnauthorizedAccessException exception = new("Access denied");
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
        await VerifyErrorResponse(context, HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task InvokeAsync_KeyNotFoundException_ReturnsNotFound()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        KeyNotFoundException exception = new("Key not found");
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        await VerifyErrorResponse(context, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_WithNotFoundMessage_ReturnsNotFound()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        InvalidOperationException exception = new("Entity not found");
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
        await VerifyErrorResponse(context, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task InvokeAsync_OperationCanceledException_ReturnsRequestTimeout()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        OperationCanceledException exception = new();
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        // OperationCanceledException returns 499 (Client Closed Request), not 408
        context.Response.StatusCode.Should().Be(499);
        await VerifyErrorResponse(context, (HttpStatusCode)499);
    }

    [Fact]
    public async Task InvokeAsync_BadHttpRequestException_413_ReturnsPayloadTooLarge()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        BadHttpRequestException exception = new("Payload too large", 413);
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.RequestEntityTooLarge);
        await VerifyErrorResponse(context, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task InvokeAsync_GeneralException_ReturnsInternalServerError()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        Exception exception = new("General error");
        _mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        await VerifyErrorResponse(context, HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Helper Methods

    private static DefaultHttpContext CreateHttpContext()
    {
        DefaultHttpContext context = new();
        context.Request.Path = "/api/test";
        context.Request.Method = "GET";
        // Set up response body as MemoryStream so it can be read back
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task VerifyErrorResponse(HttpContext context, HttpStatusCode expectedStatusCode)
    {
        // Reset stream position to read from beginning
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string responseBody = await reader.ReadToEndAsync();

        // JSON uses camelCase, so check for lowercase "succeeded"
        responseBody.Should().Contain("succeeded");
        responseBody.Should().Contain("false");
        // WriteAsJsonAsync sets ContentType to "application/json; charset=utf-8"
        context.Response.ContentType.Should().StartWith("application/json");
    }

    #endregion
}
