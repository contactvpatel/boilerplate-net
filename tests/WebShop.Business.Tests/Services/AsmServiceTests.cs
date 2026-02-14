using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Business.DTOs;
using WebShop.Business.Services;
using WebShop.Core.Interfaces.Base;
using WebShop.Core.Models;
using Xunit;

namespace WebShop.Business.Tests.Services;

/// <summary>
/// Unit tests for AsmService.
/// </summary>
[Trait("Category", "Unit")]
public class AsmServiceTests
{
    private readonly Mock<Core.Interfaces.Services.IAsmService> mockCoreService = new();
    private readonly Mock<ICacheService> mockCacheService = new();
    private readonly Mock<ILogger<AsmService>> mockLogger = new();

    private readonly AsmService service;

    public AsmServiceTests()
    {
        // Cache miss: invoke the factory so the core service is called and result is returned
        mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<List<AsmResponseDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<List<AsmResponseDto>>>, TimeSpan?, TimeSpan?, CancellationToken>(
                (_, factory, _, _, cancellationToken) => factory(cancellationToken));

        service = new AsmService(mockCoreService.Object, mockCacheService.Object, mockLogger.Object);
    }

    #region GetApplicationSecurityAsync Tests

    [Fact]
    public async Task GetApplicationSecurityAsync_ValidPersonIdAndToken_ReturnsSecurityInfo()
    {
        // Arrange - core returns list of AsmResponseModel; Business layer maps to DTOs
        const string personId = "person-123";
        const string token = "valid-token";
        IReadOnlyList<AsmResponseModel> coreResult =
        [
            new AsmResponseModel
            {
                RoleId = 1,
                PositionId = 1,
                ApplicationAccess =
                [
                    new ApplicationAccessModel { ModuleCode = "app-1", ModuleName = "App 1", HasViewAccess = true },
                    new ApplicationAccessModel { ModuleCode = "app-2", ModuleName = "App 2", HasCreateAccess = true }
                ]
            }
        ];

        mockCoreService
            .Setup(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coreResult);

        // Act
        IReadOnlyList<AsmResponseDto> result = await service.GetApplicationSecurityAsync(personId, token);

        // Assert - Business returns structured list only
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ApplicationAccess.Should().HaveCount(2);
        result[0].ApplicationAccess.ElementAt(0).ModuleCode.Should().Be("app-1");
        result[0].ApplicationAccess.ElementAt(1).ModuleCode.Should().Be("app-2");
        mockCoreService.Verify(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetApplicationSecurityAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange - core returns empty list; Business layer maps to empty list
        const string personId = "person-123";
        const string token = "valid-token";
        IReadOnlyList<AsmResponseModel> coreResult = [];

        mockCoreService
            .Setup(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coreResult);

        // Act
        IReadOnlyList<AsmResponseDto> result = await service.GetApplicationSecurityAsync(personId, token);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetApplicationSecurityAsync_NullPersonId_ThrowsArgumentException()
    {
        // Arrange
        const string? personId = null;
        const string token = "valid-token";

        // Act
        Func<Task> act = async () => await service.GetApplicationSecurityAsync(personId!, token);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        mockCoreService.Verify(s => s.GetApplicationSecurityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetApplicationSecurityAsync_EmptyPersonId_ThrowsArgumentException()
    {
        // Arrange
        const string personId = "";
        const string token = "valid-token";

        // Act
        Func<Task> act = async () => await service.GetApplicationSecurityAsync(personId, token);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetApplicationSecurityAsync_WhitespacePersonId_ThrowsArgumentException()
    {
        // Arrange
        const string personId = "   ";
        const string token = "valid-token";

        // Act
        Func<Task> act = async () => await service.GetApplicationSecurityAsync(personId, token);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetApplicationSecurityAsync_NullToken_ThrowsArgumentException()
    {
        // Arrange
        const string personId = "person-123";
        const string? token = null;

        // Act
        Func<Task> act = async () => await service.GetApplicationSecurityAsync(personId, token!);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetApplicationSecurityAsync_EmptyToken_ThrowsArgumentException()
    {
        // Arrange
        const string personId = "person-123";
        const string token = "";

        // Act
        Func<Task> act = async () => await service.GetApplicationSecurityAsync(personId, token);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task GetApplicationSecurityAsync_WhitespaceToken_ThrowsArgumentException()
    {
        // Arrange
        const string personId = "person-123";
        const string token = "   ";

        // Act
        Func<Task> act = async () => await service.GetApplicationSecurityAsync(personId, token);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
