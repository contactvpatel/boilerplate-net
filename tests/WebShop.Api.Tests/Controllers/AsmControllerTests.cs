using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Controllers;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Core.Interfaces.Base;
using Xunit;
using IAsmService = WebShop.Business.Services.Interfaces.IAsmService;

namespace WebShop.Api.Tests.Controllers;

/// <summary>
/// Unit tests for AsmController.
/// </summary>
[Trait("Category", "Unit")]
public class AsmControllerTests
{
    private readonly Mock<IAsmService> _mockService;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ILogger<AsmController>> _mockLogger;
    private readonly AsmController _controller;

    public AsmControllerTests()
    {
        _mockService = new Mock<IAsmService>();
        _mockUserContext = new Mock<IUserContext>();
        _mockLogger = new Mock<ILogger<AsmController>>();
        _controller = new AsmController(
            _mockService.Object,
            _mockUserContext.Object,
            _mockLogger.Object);
    }

    #region Get Tests

    [Fact]
    public async Task Get_ValidPersonIdAndToken_ReturnsOk()
    {
        // Arrange
        const string personId = "person-123";
        const string token = "valid-token";
        List<AsmResponseDto> securityInfo = new List<AsmResponseDto>
        {
            new()
            {
                ApplicationId = "app-1",
                ApplicationName = "App 1",
                Permissions = new List<string> { "read", "write" },
                HasAccess = true
            },
            new()
            {
                ApplicationId = "app-2",
                ApplicationName = "App 2",
                Permissions = new List<string> { "read" },
                HasAccess = true
            }
        };

        _mockUserContext.Setup(u => u.GetUserId()).Returns(personId);
        _mockUserContext.Setup(u => u.GetToken()).Returns(token);
        _mockService
            .Setup(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(securityInfo);

        // Act
        ActionResult<Response<IReadOnlyList<AsmResponseDto>>> result = await _controller.Get(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<AsmResponseDto>>? response = okResult!.Value as Response<IReadOnlyList<AsmResponseDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Get_MissingPersonId_ReturnsUnauthorized()
    {
        // Arrange
        const string token = "valid-token";
        _mockUserContext.Setup(u => u.GetUserId()).Returns((string?)null);
        _mockUserContext.Setup(u => u.GetToken()).Returns(token);

        // Act
        ActionResult<Response<IReadOnlyList<AsmResponseDto>>> result = await _controller.Get(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        UnauthorizedObjectResult? unauthorizedResult = result.Result as UnauthorizedObjectResult;
        Response<IReadOnlyList<AsmResponseDto>>? response = unauthorizedResult!.Value as Response<IReadOnlyList<AsmResponseDto>>;
        response!.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Get_MissingToken_ReturnsUnauthorized()
    {
        // Arrange
        const string personId = "person-123";
        _mockUserContext.Setup(u => u.GetUserId()).Returns(personId);
        _mockUserContext.Setup(u => u.GetToken()).Returns((string?)null);

        // Act
        ActionResult<Response<IReadOnlyList<AsmResponseDto>>> result = await _controller.Get(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        UnauthorizedObjectResult? unauthorizedResult = result.Result as UnauthorizedObjectResult;
        Response<IReadOnlyList<AsmResponseDto>>? response = unauthorizedResult!.Value as Response<IReadOnlyList<AsmResponseDto>>;
        response!.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task Get_NoSecurityInfo_ReturnsOkWithEmptyList()
    {
        // Arrange
        const string personId = "person-123";
        const string token = "valid-token";
        _mockUserContext.Setup(u => u.GetUserId()).Returns(personId);
        _mockUserContext.Setup(u => u.GetToken()).Returns(token);
        _mockService
            .Setup(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AsmResponseDto>());

        // Act
        ActionResult<Response<IReadOnlyList<AsmResponseDto>>> result = await _controller.Get(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<AsmResponseDto>>? response = okResult!.Value as Response<IReadOnlyList<AsmResponseDto>>;
        response!.Data.Should().BeEmpty();
        response.Succeeded.Should().BeTrue();
    }

    #endregion
}
