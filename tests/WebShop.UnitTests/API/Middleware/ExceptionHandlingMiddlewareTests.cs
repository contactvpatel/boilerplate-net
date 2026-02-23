using System.Net;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Middleware;
using WebShop.Api.Models;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.API.Middleware;

/// <summary>
/// Unit tests for ExceptionHandlingMiddleware.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class ExceptionHandlingMiddlewareTests
{
    private readonly Mock<RequestDelegate> mockNext = new();
    private readonly Mock<ILogger<ExceptionHandlingMiddleware>> mockLogger = new();
    private readonly ExceptionHandlingOptions options = new();
    private readonly ExceptionHandlingMiddleware middleware;

    public ExceptionHandlingMiddlewareTests()
    {
        ExceptionHandlingStrategy strategy = new();
        middleware = new ExceptionHandlingMiddleware(options, strategy, mockNext.Object, mockLogger.Object);
    }

    #region InvokeAsync Tests

    [Fact]
    public async Task InvokeAsync_NoException_CallsNext()
    {
        // Arrange
        DefaultHttpContext context = new();
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).Returns(Task.CompletedTask);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        mockNext.Verify(n => n(It.IsAny<HttpContext>()), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ArgumentException_ReturnsBadRequest()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        ArgumentException exception = new("Invalid argument");
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

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
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

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
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

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
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

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
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

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
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

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
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.RequestEntityTooLarge);
        await VerifyErrorResponse(context, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task InvokeAsync_BadHttpRequestException_413_WithExceedsMessage_ReturnsPayloadTooLarge()
    {
        // Arrange - tests GetPayloadTooLargeMessage "exceeds" branch
        DefaultHttpContext context = CreateHttpContext();
        BadHttpRequestException exception = new("Request size exceeds the limit", 413);
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.RequestEntityTooLarge);
        await VerifyErrorResponse(context, HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task InvokeAsync_BadHttpRequestException_413_WithGenericMessage_ReturnsDefaultMessage()
    {
        // Arrange - tests GetPayloadTooLargeMessage default branch (neither "Request body too large" nor "exceeds")
        DefaultHttpContext context = CreateHttpContext();
        BadHttpRequestException exception = new("Payload too big", 413);
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.RequestEntityTooLarge);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        body.Should().Contain("exceeds the maximum allowed limit");
    }

    [Fact]
    public async Task InvokeAsync_ApplicationException_ReturnsInternalServerErrorWithMessage()
    {
        // Arrange - ApplicationException falls to GetDefaultHandling, returns message
        DefaultHttpContext context = CreateHttpContext();
        ApplicationException exception = new("Application error");
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        body.Should().Contain("Application error");
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_WithoutNotFoundMessage_ReturnsInternalServerError()
    {
        // Arrange - InvalidOperationException without "not found" falls to GetDefaultHandling
        DefaultHttpContext context = CreateHttpContext();
        InvalidOperationException exception = new("Invalid state");
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        await VerifyErrorResponse(context, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task InvokeAsync_GeneralException_ReturnsInternalServerError()
    {
        // Arrange
        DefaultHttpContext context = CreateHttpContext();
        Exception exception = new("General error");
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        await VerifyErrorResponse(context, HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task InvokeAsync_ExceptionWithExistingErrorIdInData_ResponseContainsExistingErrorId()
    {
        // Arrange - covers GetOrCreateErrorId branch when exception.Data["ErrorId"] is already set
        DefaultHttpContext context = CreateHttpContext();
        Exception exception = new("Some error");
        exception.Data["ErrorId"] = "existing-error-id-12345";
        mockNext.Setup(n => n(It.IsAny<HttpContext>())).ThrowsAsync(exception);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using StreamReader reader = new(context.Response.Body, Encoding.UTF8, leaveOpen: true);
        string body = await reader.ReadToEndAsync();
        body.Should().Contain("existing-error-id-12345");
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
