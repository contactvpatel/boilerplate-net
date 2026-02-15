using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
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
[Trait("Category", TestCategories.Unit)]
public class DatabaseMigrationHostedServiceTests
{
    [Fact]
    public async Task StartAsync_WithMigrationDisabled_CompletesWithoutError()
    {
        // Arrange
        Mock<IOptionsMonitor<AppSettingModel>> mockOptions = new();
        mockOptions.Setup(o => o.CurrentValue).Returns(new AppSettingModel { EnableDatabaseMigration = false });
        Mock<IConfiguration> mockConfiguration = new();
        Mock<IHostApplicationLifetime> mockHostLifetime = new();
        Mock<ILogger<DatabaseMigrationHostedService>> mockLogger = new();

        DatabaseMigrationHostedService service = new(
            mockOptions.Object,
            mockConfiguration.Object,
            mockHostLifetime.Object,
            mockLogger.Object);

        // Act
        Func<Task> act = () => service.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task StartAsync_WithMigrationEnabled_AndNoConnectionSettings_CompletesWithoutThrowing()
    {
        // Arrange - empty config means databaseConnectionSettings is null; service skips migration
        Mock<IOptionsMonitor<AppSettingModel>> mockOptions = new();
        mockOptions.Setup(o => o.CurrentValue).Returns(new AppSettingModel
        {
            EnableDatabaseMigration = true,
            ApplicationName = "Test"
        });

        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Mock<IHostApplicationLifetime> mockHostLifetime = new();
        Mock<ILogger<DatabaseMigrationHostedService>> mockLogger = new();

        DatabaseMigrationHostedService service = new(
            mockOptions.Object,
            configuration,
            mockHostLifetime.Object,
            mockLogger.Object);

        // Act & Assert - service skips migration when connection settings not found
        Func<Task> act = () => service.StartAsync(CancellationToken.None);
        await act.Should().NotThrowAsync();
    }
}
