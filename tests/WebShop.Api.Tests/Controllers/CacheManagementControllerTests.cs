using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Controllers;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Core.Interfaces.Base;
using Xunit;

namespace WebShop.Api.Tests.Controllers;

/// <summary>
/// Unit tests for CacheManagementController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class CacheManagementControllerTests
{
    private readonly Mock<ICacheService> mockCacheService = new();
    private readonly Mock<ILogger<CacheManagementController>> mockLogger = new();
    private readonly CacheManagementController controller;

    public CacheManagementControllerTests()
    {
        controller = new CacheManagementController(mockCacheService.Object, mockLogger.Object);
    }

    #region ClearByKeys Tests

    [Fact]
    public async Task ClearByKeys_ValidKeys_ReturnsOk()
    {
        // Arrange
        List<string> keys = new List<string> { "key1", "key2", "key3" };
        ClearCacheByKeysRequest request = new ClearCacheByKeysRequest { Keys = keys };

        mockCacheService
            .Setup(c => c.RemoveAsync(keys, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        ActionResult<Response<CacheOperationResultDto>> result = await controller.ClearByKeys(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<CacheOperationResultDto>? response = okResult!.Value as Response<CacheOperationResultDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.IsSuccess.Should().BeTrue();
        response.Data.EntriesAffected.Should().Be(3);
        response.Succeeded.Should().BeTrue();
        mockCacheService.Verify(c => c.RemoveAsync(keys, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearByKeys_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        List<string> keys = new List<string> { "key1" };
        ClearCacheByKeysRequest request = new ClearCacheByKeysRequest { Keys = keys };

        mockCacheService
            .Setup(c => c.RemoveAsync(keys, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        ActionResult<Response<CacheOperationResultDto>> result = await controller.ClearByKeys(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        Response<CacheOperationResultDto>? response = objectResult.Value as Response<CacheOperationResultDto>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion

    #region ClearByTag Tests

    [Fact]
    public async Task ClearByTag_ValidTag_ReturnsOk()
    {
        // Arrange
        const string tag = "user-cache";
        ClearCacheByTagRequest request = new ClearCacheByTagRequest { Tag = tag };

        mockCacheService
            .Setup(c => c.RemoveByTagAsync(tag, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        ActionResult<Response<CacheOperationResultDto>> result = await controller.ClearByTag(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<CacheOperationResultDto>? response = okResult!.Value as Response<CacheOperationResultDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.IsSuccess.Should().BeTrue();
        response.Succeeded.Should().BeTrue();
        mockCacheService.Verify(c => c.RemoveByTagAsync(tag, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearByTag_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        const string tag = "user-cache";
        ClearCacheByTagRequest request = new ClearCacheByTagRequest { Tag = tag };

        mockCacheService
            .Setup(c => c.RemoveByTagAsync(tag, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        ActionResult<Response<CacheOperationResultDto>> result = await controller.ClearByTag(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        Response<CacheOperationResultDto>? response = objectResult.Value as Response<CacheOperationResultDto>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion

    #region ClearByTags Tests

    [Fact]
    public async Task ClearByTags_ValidTags_ReturnsOk()
    {
        // Arrange
        List<string> tags = new List<string> { "tag1", "tag2" };
        ClearCacheByTagsRequest request = new ClearCacheByTagsRequest { Tags = tags };

        mockCacheService
            .Setup(c => c.RemoveByTagAsync(tags, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        ActionResult<Response<CacheOperationResultDto>> result = await controller.ClearByTags(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<CacheOperationResultDto>? response = okResult!.Value as Response<CacheOperationResultDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.IsSuccess.Should().BeTrue();
        response.Succeeded.Should().BeTrue();
        mockCacheService.Verify(c => c.RemoveByTagAsync(tags, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearByTags_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        List<string> tags = new List<string> { "tag1" };
        ClearCacheByTagsRequest request = new ClearCacheByTagsRequest { Tags = tags };

        mockCacheService
            .Setup(c => c.RemoveByTagAsync(tags, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        ActionResult<Response<CacheOperationResultDto>> result = await controller.ClearByTags(request, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        Response<CacheOperationResultDto>? response = objectResult.Value as Response<CacheOperationResultDto>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion

    #region ClearByKey Tests

    [Fact]
    public async Task ClearByKey_ValidKey_ReturnsOk()
    {
        // Arrange
        const string key = "cache-key-1";

        mockCacheService
            .Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        ActionResult<Response<CacheOperationResultDto>> result = await controller.ClearByKey(key, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<CacheOperationResultDto>? response = okResult!.Value as Response<CacheOperationResultDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.IsSuccess.Should().BeTrue();
        response.Data.EntriesAffected.Should().Be(1);
        response.Succeeded.Should().BeTrue();
        mockCacheService.Verify(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ClearByKey_EmptyKey_ReturnsBadRequest()
    {
        // Arrange
        const string key = "";

        // Act
        ActionResult<Response<CacheOperationResultDto>> result = await controller.ClearByKey(key, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        BadRequestObjectResult? badRequestResult = result.Result as BadRequestObjectResult;
        Response<CacheOperationResultDto>? response = badRequestResult!.Value as Response<CacheOperationResultDto>;
        response!.Succeeded.Should().BeFalse();
        mockCacheService.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClearByKey_NullKey_ReturnsBadRequest()
    {
        // Arrange
        const string? key = null;

        // Act
        ActionResult<Response<CacheOperationResultDto>> result = await controller.ClearByKey(key!, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
        BadRequestObjectResult? badRequestResult = result.Result as BadRequestObjectResult;
        Response<CacheOperationResultDto>? response = badRequestResult!.Value as Response<CacheOperationResultDto>;
        response!.Succeeded.Should().BeFalse();
        mockCacheService.Verify(c => c.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ClearByKey_ExceptionThrown_ReturnsInternalServerError()
    {
        // Arrange
        const string key = "cache-key-1";

        mockCacheService
            .Setup(c => c.RemoveAsync(key, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Cache error"));

        // Act
        ActionResult<Response<CacheOperationResultDto>> result = await controller.ClearByKey(key, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<ObjectResult>();
        ObjectResult? objectResult = result.Result as ObjectResult;
        objectResult!.StatusCode.Should().Be(500);
        Response<CacheOperationResultDto>? response = objectResult.Value as Response<CacheOperationResultDto>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion
}
