using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Controllers;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Core.Interfaces.Base;
using WebShop.UnitTests.Common;
using Xunit;
using IAsmService = WebShop.Business.Services.Interfaces.IAsmService;

namespace WebShop.UnitTests.API.Controllers;

/// <summary>
/// Unit tests for AsmController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class AsmControllerTests
{
    private readonly Mock<IAsmService> mockService = new();
    private readonly Mock<IUserContext> mockUserContext = new();
    private readonly Mock<ILogger<AsmController>> mockLogger = new();
    private readonly AsmController controller;

    public AsmControllerTests()
    {
        controller = new AsmController(mockService.Object, mockUserContext.Object, mockLogger.Object);
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
                RoleId = 111,
                PositionId = 111,
                ApplicationAccess = new List<ApplicationAccessDto>
                {
                    new() { ModuleCode = "SHR", ModuleName = "Share Report", HasViewAccess = true }
                }
            }
        };

        mockUserContext.Setup(u => u.GetUserId()).Returns(personId);
        mockUserContext.Setup(u => u.GetToken()).Returns(token);
        mockService
            .Setup(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(securityInfo);

        // Act
        ActionResult<Response<IReadOnlyList<AsmResponseDto>>> result = await controller.Get(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<AsmResponseDto>>? response = okResult!.Value as Response<IReadOnlyList<AsmResponseDto>>;
        response!.Data.Should().HaveCount(1);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Get_MissingPersonId_ReturnsUnauthorized()
    {
        // Arrange
        const string token = "valid-token";
        mockUserContext.Setup(u => u.GetUserId()).Returns((string?)null);
        mockUserContext.Setup(u => u.GetToken()).Returns(token);

        // Act
        ActionResult<Response<IReadOnlyList<AsmResponseDto>>> result = await controller.Get(CancellationToken.None);

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
        mockUserContext.Setup(u => u.GetUserId()).Returns(personId);
        mockUserContext.Setup(u => u.GetToken()).Returns((string?)null);

        // Act
        ActionResult<Response<IReadOnlyList<AsmResponseDto>>> result = await controller.Get(CancellationToken.None);

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
        mockUserContext.Setup(u => u.GetUserId()).Returns(personId);
        mockUserContext.Setup(u => u.GetToken()).Returns(token);
        mockService
            .Setup(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AsmResponseDto>());

        // Act
        ActionResult<Response<IReadOnlyList<AsmResponseDto>>> result = await controller.Get(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<AsmResponseDto>>? response = okResult!.Value as Response<IReadOnlyList<AsmResponseDto>>;
        response!.Data.Should().BeEmpty();
        response.Succeeded.Should().BeTrue();
    }

    #endregion
}
