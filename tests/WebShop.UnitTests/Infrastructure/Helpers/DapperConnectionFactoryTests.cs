using System.Data;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using WebShop.Infrastructure.Helpers;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.Infrastructure.Helpers;

/// <summary>
/// Unit tests for DapperConnectionFactory.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class DapperConnectionFactoryTests
{
    private readonly Mock<ILogger<DapperConnectionFactory>> _mockLogger;
    private readonly IConfiguration _configuration;

    public DapperConnectionFactoryTests()
    {
        _mockLogger = new Mock<ILogger<DapperConnectionFactory>>();

        // Create in-memory configuration
        Dictionary<string, string?> configData = new()
        {
            { "DatabaseConnectionSettings:Read:Host", "localhost" },
            { "DatabaseConnectionSettings:Read:Port", "5432" },
            { "DatabaseConnectionSettings:Read:DatabaseName", "testdb_read" },
            { "DatabaseConnectionSettings:Read:UserId", "testuser" },
            { "DatabaseConnectionSettings:Read:Password", "testpass" },
            { "DatabaseConnectionSettings:Write:Host", "localhost" },
            { "DatabaseConnectionSettings:Write:Port", "5432" },
            { "DatabaseConnectionSettings:Write:DatabaseName", "testdb_write" },
            { "DatabaseConnectionSettings:Write:UserId", "testuser" },
            { "DatabaseConnectionSettings:Write:Password", "testpass" }
        };

        ConfigurationBuilder builder = new();
        builder.AddInMemoryCollection(configData);
        _configuration = builder.Build();
    }

    #region CreateReadConnection Tests

    [Fact]
    public void CreateReadConnection_ValidConfiguration_ReturnsConnection()
    {
        // Arrange
        DapperConnectionFactory factory = new(_configuration, _mockLogger.Object);

        // Act
        IDbConnection connection = factory.CreateReadConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<NpgsqlConnection>();
        connection.ConnectionString.Should().Contain("testdb_read");
    }

    [Fact]
    public void CreateReadConnection_ReturnsNewConnectionEachTime()
    {
        // Arrange
        DapperConnectionFactory factory = new(_configuration, _mockLogger.Object);

        // Act
        IDbConnection connection1 = factory.CreateReadConnection();
        IDbConnection connection2 = factory.CreateReadConnection();

        // Assert
        connection1.Should().NotBeSameAs(connection2);
    }

    #endregion

    #region CreateWriteConnection Tests

    [Fact]
    public void CreateWriteConnection_ValidConfiguration_ReturnsConnection()
    {
        // Arrange
        DapperConnectionFactory factory = new(_configuration, _mockLogger.Object);

        // Act
        IDbConnection connection = factory.CreateWriteConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<NpgsqlConnection>();
        connection.ConnectionString.Should().Contain("testdb_write");
    }

    [Fact]
    public void CreateWriteConnection_ReturnsNewConnectionEachTime()
    {
        // Arrange
        DapperConnectionFactory factory = new(_configuration, _mockLogger.Object);

        // Act
        IDbConnection connection1 = factory.CreateWriteConnection();
        IDbConnection connection2 = factory.CreateWriteConnection();

        // Assert
        connection1.Should().NotBeSameAs(connection2);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void CreateReadConnection_UsingDbConnectionSettingsFallback_ReturnsConnection()
    {
        // Arrange - use DbConnectionSettings when DatabaseConnectionSettings is empty
        Dictionary<string, string?> configData = new()
        {
            { "DbConnectionSettings:Read:Host", "localhost" },
            { "DbConnectionSettings:Read:DatabaseName", "testdb_read" },
            { "DbConnectionSettings:Read:UserId", "user" },
            { "DbConnectionSettings:Read:Password", "pass" },
            { "DbConnectionSettings:Write:Host", "localhost" },
            { "DbConnectionSettings:Write:DatabaseName", "testdb_write" },
            { "DbConnectionSettings:Write:UserId", "user" },
            { "DbConnectionSettings:Write:Password", "pass" }
        };
        ConfigurationBuilder builder = new();
        builder.AddInMemoryCollection(configData);
        IConfiguration config = builder.Build();

        // Act
        DapperConnectionFactory factory = new(config, _mockLogger.Object);
        IDbConnection connection = factory.CreateReadConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.ConnectionString.Should().Contain("testdb_read");
    }

    [Fact]
    public void CreateReadConnection_WithCustomApplicationName_UsesApplicationName()
    {
        // Arrange
        Dictionary<string, string?> configData = new()
        {
            { "DatabaseConnectionSettings:Read:Host", "localhost" },
            { "DatabaseConnectionSettings:Read:DatabaseName", "testdb" },
            { "DatabaseConnectionSettings:Read:UserId", "user" },
            { "DatabaseConnectionSettings:Read:Password", "pass" },
            { "DatabaseConnectionSettings:Write:Host", "localhost" },
            { "DatabaseConnectionSettings:Write:DatabaseName", "testdb" },
            { "DatabaseConnectionSettings:Write:UserId", "user" },
            { "DatabaseConnectionSettings:Write:Password", "pass" },
            { "AppSettings:ApplicationName", "CustomApp" } // ConfigurationKeys.AppSettingsApplicationName
        };
        ConfigurationBuilder builder = new();
        builder.AddInMemoryCollection(configData);
        IConfiguration config = builder.Build();

        // Act
        DapperConnectionFactory factory = new(config, _mockLogger.Object);
        IDbConnection connection = factory.CreateReadConnection();

        // Assert
        connection.ConnectionString.Should().Contain("Application Name=CustomApp");
    }

    [Fact]
    public void Constructor_MissingConfiguration_ThrowsArgumentNullException()
    {
        // Arrange - null config section causes ArgumentNullException when ConfigurationBinder.Get is called on null
        Mock<IConfiguration> emptyConfig = new();
        emptyConfig.Setup(c => c.GetSection("DatabaseConnectionSettings")).Returns(Mock.Of<IConfigurationSection>());
        emptyConfig.Setup(c => c.GetSection("DbConnectionSettings")).Returns(() => (IConfigurationSection?)null!); // null section triggers ArgumentNullException in ConfigurationBinder.Get

        // Act & Assert - null section leads to ArgumentNullException from ConfigurationBinder.Get
        Assert.Throws<ArgumentNullException>(() =>
            new DapperConnectionFactory(emptyConfig.Object, _mockLogger.Object));
    }

    [Fact]
    public void CreateReadConnection_WithNullLogger_ReturnsConnection()
    {
        // Arrange - logger=null exercises logger?.LogDebug branch (no-op when null)
        DapperConnectionFactory factory = new(_configuration, logger: null);

        // Act
        IDbConnection connection = factory.CreateReadConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<NpgsqlConnection>();
    }

    [Fact]
    public void CreateWriteConnection_WithNullLogger_ReturnsConnection()
    {
        // Arrange - logger=null exercises logger?.LogDebug branch
        DapperConnectionFactory factory = new(_configuration, logger: null);

        // Act
        IDbConnection connection = factory.CreateWriteConnection();

        // Assert
        connection.Should().NotBeNull();
        connection.Should().BeOfType<NpgsqlConnection>();
    }

    #endregion
}
