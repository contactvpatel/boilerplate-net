using System.Data;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Npgsql;
using WebShop.Infrastructure.Helpers;
using WebShop.Util.Models;
using Xunit;

namespace WebShop.Infrastructure.Tests.Helpers;

/// <summary>
/// Unit tests for DapperConnectionFactory.
/// </summary>
[Trait("Category", "Unit")]
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
    public void Constructor_MissingConfiguration_ThrowsInvalidOperationException()
    {
        // Arrange
        Mock<IConfiguration> emptyConfig = new();
        emptyConfig.Setup(c => c.GetSection("DatabaseConnectionSettings")).Returns(Mock.Of<IConfigurationSection>());

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
            new DapperConnectionFactory(emptyConfig.Object, _mockLogger.Object));
    }

    #endregion
}
