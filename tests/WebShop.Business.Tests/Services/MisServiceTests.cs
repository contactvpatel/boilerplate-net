using FluentAssertions;
using Moq;
using WebShop.Business.DTOs;
using WebShop.Business.Mappings;
using WebShop.Business.Services;
using WebShop.Core.Interfaces.Base;
using WebShop.Core.Models;
using Xunit;

namespace WebShop.Business.Tests.Services;

/// <summary>
/// Unit tests for MisService.
/// Tests cover basic functionality, mapping, and caching per testing-strategy.md.
/// </summary>
[Trait("Category", "Unit")]
public class MisServiceTests
{
    private readonly Mock<Core.Interfaces.Services.IMisService> _mockCoreService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly MisService _service;

    public MisServiceTests()
    {
        _mockCoreService = new Mock<Core.Interfaces.Services.IMisService>();
        _mockCacheService = new Mock<ICacheService>();
        _service = new MisService(_mockCoreService.Object, _mockCacheService.Object);
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task GetAllDepartmentsAsync_ValidDivisionId_ReturnsOrderedDepartments()
    {
        // Arrange
        const int divisionId = 1;
        List<DepartmentModel> models = new List<DepartmentModel>
        {
            new() { Id = 2, Name = "Beta Department", DivisionId = divisionId },
            new() { Id = 1, Name = "Alpha Department", DivisionId = divisionId }
        };

        _mockCoreService
            .Setup(s => s.GetAllDepartmentsAsync(divisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        // Mock cache to always return the factory result
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<DepartmentDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<IReadOnlyList<DepartmentDto>>>, TimeSpan?, TimeSpan?, CancellationToken>(
                async (key, factory, exp, localExp, ct) =>
                    // Always call the factory (simulate cache miss)
                    await factory(ct));

        // Act
        IReadOnlyList<DepartmentDto> result = await _service.GetAllDepartmentsAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha Department"); // Should be ordered by name
        result[1].Name.Should().Be("Beta Department");
    }

    [Fact]
    public async Task GetAllDepartmentsAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        const int divisionId = 1;
        List<DepartmentModel> emptyModels = new List<DepartmentModel>();

        _mockCoreService
            .Setup(s => s.GetAllDepartmentsAsync(divisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyModels);

        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<DepartmentDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<IReadOnlyList<DepartmentDto>>>, TimeSpan?, TimeSpan?, CancellationToken>(
                async (key, factory, exp, localExp, ct) =>
                    // Always call the factory (simulate cache miss)
                    await factory(ct));

        // Act
        IReadOnlyList<DepartmentDto> result = await _service.GetAllDepartmentsAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllDepartmentsAsync_CoreServiceThrowsException_PropagatesException()
    {
        // Arrange
        const int divisionId = 1;

        _mockCoreService
            .Setup(s => s.GetAllDepartmentsAsync(divisionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("External service error"));

        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<DepartmentDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<IReadOnlyList<DepartmentDto>>>, TimeSpan?, TimeSpan?, CancellationToken>(
                async (key, factory, exp, localExp, ct) =>
                    // Always call the factory (simulate cache miss)
                    await factory(ct));

        // Act & Assert
        Func<Task> act = async () => await _service.GetAllDepartmentsAsync(divisionId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAllRolesAsync_ValidDivisionId_ReturnsOrderedRoles()
    {
        // Arrange
        const int divisionId = 1;
        List<RoleModel> models = new List<RoleModel>
        {
            new() { Id = 2, Name = "Beta Role", DepartmentId = 1, RoleTypeId = 1, DivisionId = divisionId },
            new() { Id = 1, Name = "Alpha Role", DepartmentId = 1, RoleTypeId = 1, DivisionId = divisionId }
        };

        _mockCoreService
            .Setup(s => s.GetAllRolesAsync(divisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<RoleDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<IReadOnlyList<RoleDto>>>, TimeSpan?, TimeSpan?, CancellationToken>(
                async (key, factory, exp, localExp, ct) =>
                    // Always call the factory (simulate cache miss)
                    await factory(ct));

        // Act
        IReadOnlyList<RoleDto> result = await _service.GetAllRolesAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha Role"); // Should be ordered by name
        result[1].Name.Should().Be("Beta Role");
    }

    [Fact]
    public async Task GetAllRoleTypesAsync_ValidDivisionId_ReturnsOrderedRoleTypes()
    {
        // Arrange
        const int divisionId = 1;
        List<RoleTypeModel> models = new List<RoleTypeModel>
        {
            new() { Id = 2, Name = "Beta Role Type", DivisionId = divisionId },
            new() { Id = 1, Name = "Alpha Role Type", DivisionId = divisionId }
        };

        _mockCoreService
            .Setup(s => s.GetAllRoleTypesAsync(divisionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<RoleTypeDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<IReadOnlyList<RoleTypeDto>>>, TimeSpan?, TimeSpan?, CancellationToken>(
                async (key, factory, exp, localExp, ct) =>
                    // Always call the factory (simulate cache miss)
                    await factory(ct));

        // Act
        IReadOnlyList<RoleTypeDto> result = await _service.GetAllRoleTypesAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha Role Type"); // Should be ordered by name
        result[1].Name.Should().Be("Beta Role Type");
    }

    [Fact]
    public async Task GetRolesByDepartmentIdAsync_ValidDepartmentId_ReturnsOrderedRoles()
    {
        // Arrange
        const int departmentId = 1;
        List<RoleModel> models = new List<RoleModel>
        {
            new() { Id = 2, Name = "Beta Role", DepartmentId = departmentId, RoleTypeId = 1, DivisionId = 1 },
            new() { Id = 1, Name = "Alpha Role", DepartmentId = departmentId, RoleTypeId = 1, DivisionId = 1 }
        };

        _mockCoreService
            .Setup(s => s.GetRolesByDepartmentIdAsync(departmentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<RoleDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<IReadOnlyList<RoleDto>>>, TimeSpan?, TimeSpan?, CancellationToken>(
                async (key, factory, exp, localExp, ct) =>
                    // Always call the factory (simulate cache miss)
                    await factory(ct));

        // Act
        IReadOnlyList<RoleDto> result = await _service.GetRolesByDepartmentIdAsync(departmentId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha Role"); // Should be ordered by name
        result[1].Name.Should().Be("Beta Role");
    }

    [Fact]
    public async Task GetPositionsByRoleIdAsync_ValidRoleId_ReturnsPositions()
    {
        // Arrange
        const int roleId = 1;
        List<PositionModel> models = new List<PositionModel>
        {
            new() { Id = 1, Name = "Position 1", RoleId = roleId },
            new() { Id = 2, Name = "Position 2", RoleId = roleId }
        };

        _mockCoreService
            .Setup(s => s.GetPositionsByRoleIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        // Act
        IReadOnlyList<PositionDto> result = await _service.GetPositionsByRoleIdAsync(roleId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        _mockCoreService.Verify(s => s.GetPositionsByRoleIdAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPersonPositionsAsync_ValidPersonId_ReturnsOrderedPositions()
    {
        // Arrange
        const string personId = "person-123";
        List<PersonPositionModel> models = new List<PersonPositionModel>
        {
            new() { PersonId = personId, PositionId = 2, PositionName = "Beta Position", RoleId = 1, RoleName = "Role 1", DepartmentId = 1, DepartmentName = "Dept 1", DivisionId = 1 },
            new() { PersonId = personId, PositionId = 1, PositionName = "Alpha Position", RoleId = 1, RoleName = "Role 1", DepartmentId = 1, DepartmentName = "Dept 1", DivisionId = 1 }
        };

        _mockCoreService
            .Setup(s => s.GetPersonPositionsAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<PersonPositionDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<IReadOnlyList<PersonPositionDto>>>, TimeSpan?, TimeSpan?, CancellationToken>(
                async (key, factory, exp, localExp, ct) =>
                    // Always call the factory (simulate cache miss)
                    await factory(ct));

        // Act
        IReadOnlyList<PersonPositionDto> result = await _service.GetPersonPositionsAsync(personId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].PositionName.Should().Be("Alpha Position"); // Should be ordered by PositionName
        result[1].PositionName.Should().Be("Beta Position");
    }

    [Fact]
    public async Task GetPersonPositionsAsync_NullPersonId_ThrowsArgumentException()
    {
        // Arrange
        const string? personId = null;

        // Act & Assert
        Func<Task> act = async () => await _service.GetPersonPositionsAsync(personId!);
        await act.Should().ThrowAsync<ArgumentException>();
        _mockCoreService.Verify(s => s.GetPersonPositionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPersonPositionsAsync_EmptyPersonId_ThrowsArgumentException()
    {
        // Arrange
        const string personId = "";

        // Act & Assert
        Func<Task> act = async () => await _service.GetPersonPositionsAsync(personId);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetPersonPositionsAsync_WhitespacePersonId_ThrowsArgumentException()
    {
        // Arrange
        const string personId = "   ";

        // Act & Assert
        Func<Task> act = async () => await _service.GetPersonPositionsAsync(personId);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
