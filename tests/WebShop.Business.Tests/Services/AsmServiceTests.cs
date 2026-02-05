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
    private readonly Mock<Core.Interfaces.Services.IAsmService> _mockCoreService;
    private readonly Mock<ICacheService> _mockCacheService;
    private readonly Mock<ILogger<AsmService>> _mockLogger;
    private readonly AsmService _service;

    public AsmServiceTests()
    {
        _mockCoreService = new Mock<Core.Interfaces.Services.IAsmService>();
        _mockCacheService = new Mock<ICacheService>();
        _mockLogger = new Mock<ILogger<AsmService>>();

        // Cache miss: invoke the factory so the core service is called and result is returned
        _mockCacheService
            .Setup(c => c.GetOrCreateAsync(
                It.IsAny<string>(),
                It.IsAny<Func<CancellationToken, Task<List<AsmResponseDto>>>>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<CancellationToken>()))
            .Returns<string, Func<CancellationToken, Task<List<AsmResponseDto>>>, TimeSpan?, TimeSpan?, CancellationToken>(
                (_, factory, _, _, cancellationToken) => factory(cancellationToken));

        _service = new AsmService(_mockCoreService.Object, _mockCacheService.Object, _mockLogger.Object);
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

        _mockCoreService
            .Setup(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coreResult);

        // Act
        IReadOnlyList<AsmResponseDto> result = await _service.GetApplicationSecurityAsync(personId, token);

        // Assert - Business returns structured list only
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ApplicationAccess.Should().HaveCount(2);
        result[0].ApplicationAccess.ElementAt(0).ModuleCode.Should().Be("app-1");
        result[0].ApplicationAccess.ElementAt(1).ModuleCode.Should().Be("app-2");
        _mockCoreService.Verify(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetApplicationSecurityAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange - core returns empty list; Business layer maps to empty list
        const string personId = "person-123";
        const string token = "valid-token";
        IReadOnlyList<AsmResponseModel> coreResult = [];

        _mockCoreService
            .Setup(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(coreResult);

        // Act
        IReadOnlyList<AsmResponseDto> result = await _service.GetApplicationSecurityAsync(personId, token);

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
        Func<Task> act = async () => await _service.GetApplicationSecurityAsync(personId!, token);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
        _mockCoreService.Verify(s => s.GetApplicationSecurityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetApplicationSecurityAsync_EmptyPersonId_ThrowsArgumentException()
    {
        // Arrange
        const string personId = "";
        const string token = "valid-token";

        // Act
        Func<Task> act = async () => await _service.GetApplicationSecurityAsync(personId, token);

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
        Func<Task> act = async () => await _service.GetApplicationSecurityAsync(personId, token);

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
        Func<Task> act = async () => await _service.GetApplicationSecurityAsync(personId, token!);

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
        Func<Task> act = async () => await _service.GetApplicationSecurityAsync(personId, token);

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
        Func<Task> act = async () => await _service.GetApplicationSecurityAsync(personId, token);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
