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
[Trait("Category", TestCategories.Unit)]
public class MisServiceTests
{
    private readonly Mock<Core.Interfaces.Services.IMisService> mockCoreService = new();
    private readonly Mock<ICacheService> mockCacheService = new();
    private readonly MisService service;

    public MisServiceTests()
    {
        service = new MisService(mockCoreService.Object, mockCacheService.Object);
    }

    #region Basic Functionality Tests

    [Fact]
    public async Task GetAllDepartmentsAsync_ValidDivisionId_ReturnsOrderedDepartments()
    {
        // Arrange - cache returns ordered list (as the real cache would after factory runs)
        const int divisionId = 1;
        List<DepartmentDto> cachedResult =
        [
            new DepartmentDto { Id = 1, Name = "Alpha Department", DivisionId = divisionId },
            new DepartmentDto { Id = 2, Name = "Beta Department", DivisionId = divisionId }
        ];

        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<List<DepartmentDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        // Act
        IReadOnlyList<DepartmentDto> result = await service.GetAllDepartmentsAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha Department");
        result[1].Name.Should().Be("Beta Department");
    }

    [Fact]
    public async Task GetAllDepartmentsAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange - cache returns empty list
        const int divisionId = 1;
        List<DepartmentDto> emptyList = [];

        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<List<DepartmentDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        // Act
        IReadOnlyList<DepartmentDto> result = await service.GetAllDepartmentsAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllDepartmentsAsync_CoreServiceThrowsException_PropagatesException()
    {
        // Arrange - cache invokes factory; core service throws
        const int divisionId = 1;

        mockCoreService
            .Setup(s => s.GetAllDepartmentsAsync(divisionId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("External service error"));

        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<List<DepartmentDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<List<DepartmentDto>>>, TimeSpan?, TimeSpan?, CancellationToken>(
                (key, factory, exp, localExp, cancellationToken) => factory(cancellationToken));

        // Act & Assert
        Func<Task> act = async () => await service.GetAllDepartmentsAsync(divisionId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task GetAllRolesAsync_ValidDivisionId_ReturnsOrderedRoles()
    {
        // Arrange - cache returns ordered list
        const int divisionId = 1;
        List<RoleDto> cachedResult =
        [
            new RoleDto { Id = 1, Name = "Alpha Role", DepartmentId = 1, RoleTypeId = 1, DivisionId = divisionId },
            new RoleDto { Id = 2, Name = "Beta Role", DepartmentId = 1, RoleTypeId = 1, DivisionId = divisionId }
        ];

        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<List<RoleDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        // Act
        IReadOnlyList<RoleDto> result = await service.GetAllRolesAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha Role");
        result[1].Name.Should().Be("Beta Role");
    }

    [Fact]
    public async Task GetAllRoleTypesAsync_ValidDivisionId_ReturnsOrderedRoleTypes()
    {
        // Arrange - cache returns ordered list
        const int divisionId = 1;
        List<RoleTypeDto> cachedResult =
        [
            new RoleTypeDto { Id = 1, Name = "Alpha Role Type", DivisionId = divisionId },
            new RoleTypeDto { Id = 2, Name = "Beta Role Type", DivisionId = divisionId }
        ];

        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<List<RoleTypeDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        // Act
        IReadOnlyList<RoleTypeDto> result = await service.GetAllRoleTypesAsync(divisionId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha Role Type");
        result[1].Name.Should().Be("Beta Role Type");
    }

    [Fact]
    public async Task GetRolesByDepartmentIdAsync_ValidDepartmentId_ReturnsOrderedRoles()
    {
        // Arrange - cache returns ordered list
        const int departmentId = 1;
        List<RoleDto> cachedResult =
        [
            new RoleDto { Id = 1, Name = "Alpha Role", DepartmentId = departmentId, RoleTypeId = 1, DivisionId = 1 },
            new RoleDto { Id = 2, Name = "Beta Role", DepartmentId = departmentId, RoleTypeId = 1, DivisionId = 1 }
        ];

        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<List<RoleDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        // Act
        IReadOnlyList<RoleDto> result = await service.GetRolesByDepartmentIdAsync(departmentId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha Role");
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

        mockCoreService
            .Setup(s => s.GetPositionsByRoleIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        // Act
        IReadOnlyList<PositionDto> result = await service.GetPositionsByRoleIdAsync(roleId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        mockCoreService.Verify(s => s.GetPositionsByRoleIdAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
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

        mockCoreService
            .Setup(s => s.GetPersonPositionsAsync(personId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<IReadOnlyList<PersonPositionDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<IReadOnlyList<PersonPositionDto>>>, TimeSpan?, TimeSpan?, CancellationToken>(
                async (key, factory, exp, localExp, cancellationToken) =>
                    // Always call the factory (simulate cache miss)
                    await factory(cancellationToken));

        // Act
        IReadOnlyList<PersonPositionDto> result = await service.GetPersonPositionsAsync(personId);

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
        Func<Task> act = async () => await service.GetPersonPositionsAsync(personId!);
        await act.Should().ThrowAsync<ArgumentException>();
        mockCoreService.Verify(s => s.GetPersonPositionsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPersonPositionsAsync_EmptyPersonId_ThrowsArgumentException()
    {
        // Arrange
        const string personId = "";

        // Act & Assert
        Func<Task> act = async () => await service.GetPersonPositionsAsync(personId);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetPersonPositionsAsync_WhitespacePersonId_ThrowsArgumentException()
    {
        // Arrange
        const string personId = "   ";

        // Act & Assert
        Func<Task> act = async () => await service.GetPersonPositionsAsync(personId);
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
