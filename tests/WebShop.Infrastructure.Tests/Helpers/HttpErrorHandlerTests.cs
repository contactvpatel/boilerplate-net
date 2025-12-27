using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Infrastructure.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Helpers;

/// <summary>
/// Unit tests for HttpErrorHandler.
/// </summary>
[Trait("Category", "Unit")]
public class HttpErrorHandlerTests
{
    private readonly Mock<ILogger> _mockLogger;

    public HttpErrorHandlerTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    #region HandleResponseAndThrowAsync Tests

    [Fact]
    public async Task HandleResponseAndThrowAsync_SuccessStatusCode_DoesNotThrow()
    {
        // Arrange
        HttpResponseMessage response = new(HttpStatusCode.OK);

        // Act
        Func<Task> act = async () => await HttpErrorHandler.HandleResponseAndThrowAsync(
            response, _mockLogger.Object, "/api/test", "GET");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task HandleResponseAndThrowAsync_BadRequest_ThrowsHttpRequestException()
    {
        // Arrange
        HttpResponseMessage response = new(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"error\":\"Bad request\"}", Encoding.UTF8, "application/json")
        };

        // Act
        Func<Task> act = async () => await HttpErrorHandler.HandleResponseAndThrowAsync(
            response, _mockLogger.Object, "/api/test", "GET");

        // Assert
        (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task HandleResponseAndThrowAsync_Unauthorized_ThrowsHttpRequestException()
    {
        // Arrange
        HttpResponseMessage response = new(HttpStatusCode.Unauthorized);

        // Act
        Func<Task> act = async () => await HttpErrorHandler.HandleResponseAndThrowAsync(
            response, _mockLogger.Object, "/api/test", "GET");

        // Assert
        (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task HandleResponseAndThrowAsync_NotFound_ThrowsHttpRequestException()
    {
        // Arrange
        HttpResponseMessage response = new(HttpStatusCode.NotFound);

        // Act
        Func<Task> act = async () => await HttpErrorHandler.HandleResponseAndThrowAsync(
            response, _mockLogger.Object, "/api/test", "GET");

        // Assert
        (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task HandleResponseAndThrowAsync_InternalServerError_ThrowsHttpRequestException()
    {
        // Arrange
        HttpResponseMessage response = new(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("Server error", Encoding.UTF8, "text/plain")
        };

        // Act
        Func<Task> act = async () => await HttpErrorHandler.HandleResponseAndThrowAsync(
            response, _mockLogger.Object, "/api/test", "GET");

        // Assert
        (await act.Should().ThrowAsync<HttpRequestException>()).Which.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task HandleResponseAndThrowAsync_LargeErrorContent_TruncatesContent()
    {
        // Arrange
        string largeContent = new string('A', 20 * 1024); // 20KB
        HttpResponseMessage response = new(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(largeContent, Encoding.UTF8, "text/plain")
        };

        // Act
        Func<Task> act = async () => await HttpErrorHandler.HandleResponseAndThrowAsync(
            response, _mockLogger.Object, "/api/test", "GET");

        // Assert
        (await act.Should().ThrowAsync<HttpRequestException>()).Which.Message.Should().ContainAny("Truncated", "too large");
    }

    #endregion

    #region LogAndThrowException Tests

    [Fact]
    public void LogAndThrowException_HttpRequestException_ThrowsHttpRequestException()
    {
        // Arrange
        HttpRequestException originalException = new("HTTP error");

        // Act
        Action act = () => HttpErrorHandler.LogAndThrowException(
            originalException, _mockLogger.Object, "/api/test", "GET");

        // Assert
        act.Should().Throw<HttpRequestException>();
    }

    [Fact]
    public void LogAndThrowException_TaskCanceledException_ThrowsTaskCanceledException()
    {
        // Arrange
        TaskCanceledException originalException = new("Timeout");

        // Act
        Action act = () => HttpErrorHandler.LogAndThrowException(
            originalException, _mockLogger.Object, "/api/test", "GET");

        // Assert
        act.Should().Throw<TaskCanceledException>();
    }

    [Fact]
    public void LogAndThrowException_JsonException_ThrowsJsonException()
    {
        // Arrange
        System.Text.Json.JsonException originalException = new("JSON error");

        // Act
        Action act = () => HttpErrorHandler.LogAndThrowException(
            originalException, _mockLogger.Object, "/api/test", "GET");

        // Assert
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [Fact]
    public void LogAndThrowException_GenericException_ThrowsHttpRequestException()
    {
        // Arrange
        Exception originalException = new("Generic error");

        // Act
        Action act = () => HttpErrorHandler.LogAndThrowException(
            originalException, _mockLogger.Object, "/api/test", "GET");

        // Assert
        act.Should().Throw<HttpRequestException>();
    }

    #endregion
}
