using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebShop.Api.HostedServices;
using WebShop.Util.Models;
using Xunit;

namespace WebShop.Api.Tests.HostedServices;

/// <summary>
/// Unit tests for DatabaseMigrationHostedService.
/// </summary>
[Trait("Category", "Unit")]
public class DatabaseMigrationHostedServiceTests
{
    [Fact]
    public async Task StartAsync_WithMigrationDisabled_CompletesWithoutError()
    {
        // Arrange
        Mock<IOptionsMonitor<AppSettingModel>> mockOptions = new();
        mockOptions.Setup(o => o.CurrentValue).Returns(new AppSettingModel { EnableDatabaseMigration = false });
        Mock<IConfiguration> mockConfiguration = new();
        Mock<ILogger<DatabaseMigrationHostedService>> mockLogger = new();

        DatabaseMigrationHostedService service = new(
            mockOptions.Object,
            mockConfiguration.Object,
            mockLogger.Object);

        // Act
        Func<Task> act = () => service.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_WithMigrationEnabled_ThrowsWhenConnectionInvalid()
    {
        // Arrange
        Mock<IOptionsMonitor<AppSettingModel>> mockOptions = new();
        mockOptions.Setup(o => o.CurrentValue).Returns(new AppSettingModel
        {
            EnableDatabaseMigration = true,
            ApplicationName = "Test"
        });

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Mock<ILogger<DatabaseMigrationHostedService>> mockLogger = new();

        DatabaseMigrationHostedService service = new(
            mockOptions.Object,
            configuration,
            mockLogger.Object);

        // Act & Assert
        // The service will throw when trying to ensure database with invalid connection string
        Func<Task> act = () => service.StartAsync(CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*database name*");
    }
}
