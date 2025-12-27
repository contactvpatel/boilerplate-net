using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Controllers;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Core.Interfaces.Base;
using Xunit;
using IMisService = WebShop.Business.Services.Interfaces.IMisService;

namespace WebShop.Api.Tests.Controllers;

/// <summary>
/// Unit tests for MisController.
/// </summary>
[Trait("Category", "Unit")]
public class MisControllerTests
{
    private readonly Mock<IMisService> _mockService;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly Mock<ILogger<MisController>> _mockLogger;
    private readonly MisController _controller;

    public MisControllerTests()
    {
        _mockService = new Mock<IMisService>();
        _mockUserContext = new Mock<IUserContext>();
        _mockLogger = new Mock<ILogger<MisController>>();
        _controller = new MisController(
            _mockService.Object,
            _mockUserContext.Object,
            _mockLogger.Object);
    }

    #region GetAllDepartments Tests

    [Fact]
    public async Task GetAllDepartments_ValidDivisionId_ReturnsOk()
    {
        // Arrange
        const int divisionId = 1;
        List<DepartmentDto> departments = new List<DepartmentDto>
        {
            new() { Id = 1, Name = "Department 1", DivisionId = divisionId },
            new() { Id = 2, Name = "Department 2", DivisionId = divisionId }
        };

        _mockService
            .Setup(s => s.GetAllDepartmentsAsync(divisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(departments);

        // Act
        ActionResult<Response<IReadOnlyList<DepartmentDto>>> result = await _controller.GetAllDepartments(divisionId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<DepartmentDto>>? response = okResult!.Value as Response<IReadOnlyList<DepartmentDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllDepartments_ZeroDivisionId_DefaultsToOne()
    {
        // Arrange
        const int defaultDivisionId = 1;
        List<DepartmentDto> departments = new List<DepartmentDto>
        {
            new() { Id = 1, Name = "Department 1", DivisionId = defaultDivisionId }
        };

        _mockService
            .Setup(s => s.GetAllDepartmentsAsync(defaultDivisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(departments);

        // Act
        ActionResult<Response<IReadOnlyList<DepartmentDto>>> result = await _controller.GetAllDepartments(0, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        _mockService.Verify(s => s.GetAllDepartmentsAsync(defaultDivisionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllDepartments_NoDepartments_ReturnsNotFound()
    {
        // Arrange
        const int divisionId = 1;
        _mockService
            .Setup(s => s.GetAllDepartmentsAsync(divisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<DepartmentDto>());

        // Act
        ActionResult<Response<IReadOnlyList<DepartmentDto>>> result = await _controller.GetAllDepartments(divisionId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<IReadOnlyList<DepartmentDto>>? response = notFoundResult!.Value as Response<IReadOnlyList<DepartmentDto>>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion

    #region GetDepartmentById Tests

    [Fact]
    public async Task GetDepartmentById_ValidId_ReturnsOk()
    {
        // Arrange
        const int departmentId = 1;
        DepartmentDto department = new DepartmentDto { Id = departmentId, Name = "Department 1" };

        _mockService
            .Setup(s => s.GetDepartmentByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(department);

        // Act
        ActionResult<Response<DepartmentDto>> result = await _controller.GetDepartmentById(departmentId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<DepartmentDto>? response = okResult!.Value as Response<DepartmentDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(departmentId);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetDepartmentById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int departmentId = 999;
        _mockService
            .Setup(s => s.GetDepartmentByIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((DepartmentDto?)null);

        // Act
        ActionResult<Response<DepartmentDto>> result = await _controller.GetDepartmentById(departmentId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<DepartmentDto>? response = notFoundResult!.Value as Response<DepartmentDto>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion

    #region GetAllRoleTypes Tests

    [Fact]
    public async Task GetAllRoleTypes_ValidDivisionId_ReturnsOk()
    {
        // Arrange
        const int divisionId = 1;
        List<RoleTypeDto> roleTypes = new List<RoleTypeDto>
        {
            new() { Id = 1, Name = "RoleType 1", DivisionId = divisionId },
            new() { Id = 2, Name = "RoleType 2", DivisionId = divisionId }
        };

        _mockService
            .Setup(s => s.GetAllRoleTypesAsync(divisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roleTypes);

        // Act
        ActionResult<Response<IReadOnlyList<RoleTypeDto>>> result = await _controller.GetAllRoleTypes(divisionId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<RoleTypeDto>>? response = okResult!.Value as Response<IReadOnlyList<RoleTypeDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllRoleTypes_NoRoleTypes_ReturnsNotFound()
    {
        // Arrange
        const int divisionId = 1;
        _mockService
            .Setup(s => s.GetAllRoleTypesAsync(divisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RoleTypeDto>());

        // Act
        ActionResult<Response<IReadOnlyList<RoleTypeDto>>> result = await _controller.GetAllRoleTypes(divisionId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<IReadOnlyList<RoleTypeDto>>? response = notFoundResult!.Value as Response<IReadOnlyList<RoleTypeDto>>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion

    #region GetAllRoles Tests

    [Fact]
    public async Task GetAllRoles_ValidDivisionId_ReturnsOk()
    {
        // Arrange
        const int divisionId = 1;
        List<RoleDto> roles = new List<RoleDto>
        {
            new() { Id = 1, Name = "Role 1", DivisionId = divisionId },
            new() { Id = 2, Name = "Role 2", DivisionId = divisionId }
        };

        _mockService
            .Setup(s => s.GetAllRolesAsync(divisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        // Act
        ActionResult<Response<IReadOnlyList<RoleDto>>> result = await _controller.GetAllRoles(divisionId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<RoleDto>>? response = okResult!.Value as Response<IReadOnlyList<RoleDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetAllRoles_NoRoles_ReturnsNotFound()
    {
        // Arrange
        const int divisionId = 1;
        _mockService
            .Setup(s => s.GetAllRolesAsync(divisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RoleDto>());

        // Act
        ActionResult<Response<IReadOnlyList<RoleDto>>> result = await _controller.GetAllRoles(divisionId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<IReadOnlyList<RoleDto>>? response = notFoundResult!.Value as Response<IReadOnlyList<RoleDto>>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion

    #region GetRoleById Tests

    [Fact]
    public async Task GetRoleById_ValidId_ReturnsOk()
    {
        // Arrange
        const int roleId = 1;
        RoleDto role = new RoleDto { Id = roleId, Name = "Role 1" };

        _mockService
            .Setup(s => s.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(role);

        // Act
        ActionResult<Response<RoleDto>> result = await _controller.GetRoleById(roleId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<RoleDto>? response = okResult!.Value as Response<RoleDto>;
        response!.Data.Should().NotBeNull();
        response.Data!.Id.Should().Be(roleId);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetRoleById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int roleId = 999;
        _mockService
            .Setup(s => s.GetRoleByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleDto?)null);

        // Act
        ActionResult<Response<RoleDto>> result = await _controller.GetRoleById(roleId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<RoleDto>? response = notFoundResult!.Value as Response<RoleDto>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion

    #region GetRolesByDepartmentId Tests

    [Fact]
    public async Task GetRolesByDepartmentId_ValidDepartmentId_ReturnsOk()
    {
        // Arrange
        const int departmentId = 1;
        List<RoleDto> roles = new List<RoleDto>
        {
            new() { Id = 1, Name = "Role 1", DepartmentId = departmentId },
            new() { Id = 2, Name = "Role 2", DepartmentId = departmentId }
        };

        _mockService
            .Setup(s => s.GetRolesByDepartmentIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(roles);

        // Act
        ActionResult<Response<IReadOnlyList<RoleDto>>> result = await _controller.GetRolesByDepartmentId(departmentId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<RoleDto>>? response = okResult!.Value as Response<IReadOnlyList<RoleDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetRolesByDepartmentId_NoRoles_ReturnsNotFound()
    {
        // Arrange
        const int departmentId = 999;
        _mockService
            .Setup(s => s.GetRolesByDepartmentIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<RoleDto>());

        // Act
        ActionResult<Response<IReadOnlyList<RoleDto>>> result = await _controller.GetRolesByDepartmentId(departmentId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<IReadOnlyList<RoleDto>>? response = notFoundResult!.Value as Response<IReadOnlyList<RoleDto>>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion

    #region GetPositionsByRoleId Tests

    [Fact]
    public async Task GetPositionsByRoleId_ValidRoleId_ReturnsOk()
    {
        // Arrange
        const int roleId = 1;
        List<PositionDto> positions = new List<PositionDto>
        {
            new() { Id = 1, Name = "Position 1", RoleId = roleId },
            new() { Id = 2, Name = "Position 2", RoleId = roleId }
        };

        _mockService
            .Setup(s => s.GetPositionsByRoleIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        ActionResult<Response<IReadOnlyList<PositionDto>>> result = await _controller.GetPositionsByRoleId(roleId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<PositionDto>>? response = okResult!.Value as Response<IReadOnlyList<PositionDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetPositionsByRoleId_NoPositions_ReturnsNotFound()
    {
        // Arrange
        const int roleId = 999;
        _mockService
            .Setup(s => s.GetPositionsByRoleIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PositionDto>());

        // Act
        ActionResult<Response<IReadOnlyList<PositionDto>>> result = await _controller.GetPositionsByRoleId(roleId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<IReadOnlyList<PositionDto>>? response = notFoundResult!.Value as Response<IReadOnlyList<PositionDto>>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion

    #region GetPersonPositions Tests

    [Fact]
    public async Task GetPersonPositions_ValidUserId_ReturnsOk()
    {
        // Arrange
        const string userId = "user-123";
        List<PersonPositionDto> positions = new List<PersonPositionDto>
        {
            new() { PersonId = userId, RoleId = 1, PositionId = 1 },
            new() { PersonId = userId, RoleId = 2, PositionId = 2 }
        };

        _mockUserContext.Setup(u => u.GetUserId()).Returns(userId);
        _mockService
            .Setup(s => s.GetPersonPositionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(positions);

        // Act
        ActionResult<Response<IReadOnlyList<PersonPositionDto>>> result = await _controller.GetPersonPositions(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<PersonPositionDto>>? response = okResult!.Value as Response<IReadOnlyList<PersonPositionDto>>;
        response!.Data.Should().HaveCount(2);
        response.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetPersonPositions_MissingUserId_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserContext.Setup(u => u.GetUserId()).Returns((string?)null);

        // Act
        ActionResult<Response<IReadOnlyList<PersonPositionDto>>> result = await _controller.GetPersonPositions(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<UnauthorizedObjectResult>();
        UnauthorizedObjectResult? unauthorizedResult = result.Result as UnauthorizedObjectResult;
        Response<IReadOnlyList<PersonPositionDto>>? response = unauthorizedResult!.Value as Response<IReadOnlyList<PersonPositionDto>>;
        response!.Succeeded.Should().BeFalse();
    }

    [Fact]
    public async Task GetPersonPositions_NoPositions_ReturnsNotFound()
    {
        // Arrange
        const string userId = "user-123";
        _mockUserContext.Setup(u => u.GetUserId()).Returns(userId);
        _mockService
            .Setup(s => s.GetPersonPositionsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PersonPositionDto>());

        // Act
        ActionResult<Response<IReadOnlyList<PersonPositionDto>>> result = await _controller.GetPersonPositions(CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
        NotFoundObjectResult? notFoundResult = result.Result as NotFoundObjectResult;
        Response<IReadOnlyList<PersonPositionDto>>? response = notFoundResult!.Value as Response<IReadOnlyList<PersonPositionDto>>;
        response!.Succeeded.Should().BeFalse();
    }

    #endregion
}
