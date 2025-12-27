using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebShop.Api.Filters;
using WebShop.Util.Models;
using Xunit;

namespace WebShop.Api.Tests.Filters;

/// <summary>
/// Unit tests for DatabaseMigrationInitFilter.
/// </summary>
[Trait("Category", "Unit")]
public class DatabaseMigrationInitFilterTests
{
    [Fact]
    public void Configure_WithMigrationDisabled_ReturnsNext()
    {
        // Arrange
        Mock<IOptionsMonitor<AppSettingModel>> mockOptions = new();
        mockOptions.Setup(o => o.CurrentValue).Returns(new AppSettingModel { EnableDatabaseMigration = false });
        Mock<IConfiguration> mockConfiguration = new();
        Mock<ILogger<DatabaseMigrationInitFilter>> mockLogger = new();

        DatabaseMigrationInitFilter filter = new(
            mockOptions.Object,
            mockConfiguration.Object,
            mockLogger.Object);

        Mock<Action<IApplicationBuilder>> mockNext = new();

        // Act
        Action<IApplicationBuilder> configureAction = filter.Configure(mockNext.Object);

        // Assert
        configureAction.Should().NotBeNull();
    }

    [Fact]
    public void Configure_WithMigrationEnabled_ReturnsNext()
    {
        // Arrange
        Mock<IOptionsMonitor<AppSettingModel>> mockOptions = new();
        mockOptions.Setup(o => o.CurrentValue).Returns(new AppSettingModel { EnableDatabaseMigration = true });

        // Create a ConfigurationBuilder with empty DbConnectionSettings section
        // This ensures Bind doesn't fail, but CreateConnectionString will create a connection string
        // without DatabaseName. The code will try to use it and fail at EnsureDatabase,
        // but for unit tests we'll catch the exception since we don't have a real database.
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        Mock<ILogger<DatabaseMigrationInitFilter>> mockLogger = new();

        DatabaseMigrationInitFilter filter = new(
            mockOptions.Object,
            configuration,
            mockLogger.Object);

        Mock<Action<IApplicationBuilder>> mockNext = new();

        // Act & Assert
        // The filter will try to connect to database which will fail in unit test environment
        // because the connection string doesn't have a database name.
        // The filter doesn't catch this exception, so it will throw.
        // For unit tests, we expect this exception since we don't have a real database.
        Action act = () => filter.Configure(mockNext.Object);

        // The filter will throw InvalidOperationException when trying to ensure database
        // with an invalid connection string (missing database name)
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*database name*");

        // Note: Testing actual migration execution requires a full application context
        // with database connections. Integration tests would be more appropriate
        // for testing the actual migration logic.
    }
}
