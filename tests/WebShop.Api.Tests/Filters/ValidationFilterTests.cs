using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Filters;
using WebShop.Api.Models;
using Xunit;

namespace WebShop.Api.Tests.Filters;

/// <summary>
/// Unit tests for ValidationFilter.
/// </summary>
[Trait("Category", "Unit")]
public class ValidationFilterTests
{
    private readonly Mock<ILogger<ValidationFilter>> _mockLogger;
    private readonly ValidationFilter _filter;

    public ValidationFilterTests()
    {
        _mockLogger = new Mock<ILogger<ValidationFilter>>();
        _filter = new ValidationFilter(_mockLogger.Object);
    }

    [Fact]
    public async Task OnActionExecutionAsync_ValidModelState_ContinuesToNext()
    {
        // Arrange
        ActionExecutingContext executingContext = CreateActionExecutingContext(isValid: true);
        bool nextCalled = false;
        ActionExecutionDelegate next = async () =>
        {
            nextCalled = true;
            await Task.CompletedTask;
            return new ActionExecutedContext(executingContext, new List<IFilterMetadata>(), executingContext.Controller);
        };

        // Act
        await _filter.OnActionExecutionAsync(executingContext, next);

        // Assert
        nextCalled.Should().BeTrue();
        executingContext.Result.Should().BeNull();
    }

    [Fact]
    public async Task OnActionExecutionAsync_InvalidModelState_ReturnsBadRequest()
    {
        // Arrange
        ActionExecutingContext executingContext = CreateActionExecutingContext(isValid: false);
        bool nextCalled = false;
        ActionExecutionDelegate next = async () =>
        {
            nextCalled = true;
            await Task.CompletedTask;
            return new ActionExecutedContext(executingContext, new List<IFilterMetadata>(), executingContext.Controller);
        };

        // Act
        await _filter.OnActionExecutionAsync(executingContext, next);

        // Assert
        nextCalled.Should().BeFalse();
        executingContext.Result.Should().BeOfType<BadRequestObjectResult>();
        BadRequestObjectResult? badRequestResult = executingContext.Result as BadRequestObjectResult;
        badRequestResult!.Value.Should().BeOfType<Response<object?>>();
        Response<object?>? response = badRequestResult.Value as Response<object?>;
        response!.Succeeded.Should().BeFalse();
        response.Message.Should().Be("Validation failed");
        response.Errors.Should().NotBeNull();
        response.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task OnActionExecutionAsync_NullContext_DoesNotThrow()
    {
        // Arrange
        ActionExecutingContext? executingContext = null;
        ActionExecutionDelegate next = async () =>
        {
            await Task.CompletedTask;
            return new ActionExecutedContext(
                new ActionContext(new DefaultHttpContext(), new Microsoft.AspNetCore.Routing.RouteData(), new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()),
                new List<IFilterMetadata>(),
                new object());
        };

        // Act
        Func<Task> act = async () => await _filter.OnActionExecutionAsync(executingContext!, next);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task OnActionExecutionAsync_InvalidModelState_LogsWarning()
    {
        // Arrange
        ActionExecutingContext executingContext = CreateActionExecutingContext(isValid: false);
        ActionExecutionDelegate next = async () =>
        {
            await Task.CompletedTask;
            return new ActionExecutedContext(executingContext, new List<IFilterMetadata>(), executingContext.Controller);
        };

        // Act
        await _filter.OnActionExecutionAsync(executingContext, next);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task OnActionExecutionAsync_InvalidModelState_IncludesErrorDetails()
    {
        // Arrange
        ActionExecutingContext executingContext = CreateActionExecutingContext(isValid: false);
        ActionExecutionDelegate next = async () =>
        {
            await Task.CompletedTask;
            return new ActionExecutedContext(executingContext, new List<IFilterMetadata>(), executingContext.Controller);
        };

        // Act
        await _filter.OnActionExecutionAsync(executingContext, next);

        // Assert
        BadRequestObjectResult? badRequestResult = executingContext.Result as BadRequestObjectResult;
        Response<object?>? response = badRequestResult!.Value as Response<object?>;
        response!.Errors.Should().NotBeNull();
        response.Errors.Should().NotBeEmpty();
        response.Errors!.All(e => !string.IsNullOrEmpty(e.Message)).Should().BeTrue();
        response.Errors!.All(e => e.StatusCode == 400).Should().BeTrue();
    }

    private static ActionExecutingContext CreateActionExecutingContext(bool isValid)
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        Microsoft.AspNetCore.Routing.RouteData routeData = new Microsoft.AspNetCore.Routing.RouteData();
        routeData.Values["version"] = "1";
        routeData.Values["controller"] = "TestController";
        routeData.Values["action"] = "TestAction";

        ModelStateDictionary modelState = new ModelStateDictionary();
        if (!isValid)
        {
            modelState.AddModelError("Name", "Name is required");
            modelState.AddModelError("Email", "Email is invalid");
        }

        ActionContext actionContext = new ActionContext(
            httpContext,
            routeData,
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor(),
            modelState);

        ActionExecutingContext context = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());

        return context;
    }
}
