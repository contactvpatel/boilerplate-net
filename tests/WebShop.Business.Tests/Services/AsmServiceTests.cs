using FluentAssertions;
using Moq;
using WebShop.Business.DTOs;
using WebShop.Business.Services;
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
    private readonly AsmService _service;

    public AsmServiceTests()
    {
        _mockCoreService = new Mock<Core.Interfaces.Services.IAsmService>();
        _service = new AsmService(_mockCoreService.Object);
    }

    #region GetApplicationSecurityAsync Tests

    [Fact]
    public async Task GetApplicationSecurityAsync_ValidPersonIdAndToken_ReturnsSecurityInfo()
    {
        // Arrange
        const string personId = "person-123";
        const string token = "valid-token";
        List<AsmResponseModel> models = new List<AsmResponseModel>
        {
            new() { ApplicationId = "app-1", ApplicationName = "App 1", Permissions = new List<string> { "read" }, HasAccess = true },
            new() { ApplicationId = "app-2", ApplicationName = "App 2", Permissions = new List<string> { "write" }, HasAccess = true }
        };

        _mockCoreService
            .Setup(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

        // Act
        IReadOnlyList<AsmResponseDto> result = await _service.GetApplicationSecurityAsync(personId, token);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].ApplicationId.Should().Be("app-1");
        result[1].ApplicationId.Should().Be("app-2");
        _mockCoreService.Verify(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetApplicationSecurityAsync_EmptyResult_ReturnsEmptyList()
    {
        // Arrange
        const string personId = "person-123";
        const string token = "valid-token";
        List<AsmResponseModel> models = new List<AsmResponseModel>();

        _mockCoreService
            .Setup(s => s.GetApplicationSecurityAsync(personId, token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(models);

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
