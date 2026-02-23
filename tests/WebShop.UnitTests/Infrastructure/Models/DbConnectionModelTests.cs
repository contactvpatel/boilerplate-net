using FluentAssertions;
using WebShop.UnitTests.Common;
using WebShop.Util.Models;
using Xunit;

namespace WebShop.UnitTests.Infrastructure.Models;

/// <summary>
/// Unit tests for DbConnectionModel.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class DbConnectionModelTests
{
    #region CreateConnectionString Tests

    [Fact]
    public void CreateConnectionString_NullModel_ThrowsArgumentNullException()
    {
        // Act & Assert
        Action act = () => DbConnectionModel.CreateConnectionString(null!, "TestApp");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void CreateConnectionString_ValidModel_ReturnsConnectionString()
    {
        // Arrange
        ConnectionModel model = new()
        {
            Host = "localhost",
            DatabaseName = "testdb",
            UserId = "user",
            Password = "pass",
            Port = "5432"
        };

        // Act
        string result = DbConnectionModel.CreateConnectionString(model, "TestApp");

        // Assert
        result.Should().Contain("Host=localhost");
        result.Should().Contain("Database=testdb");
        result.Should().Contain("Username=user");
        result.Should().Contain("Password=pass");
        result.Should().Contain("Port=5432");
        result.Should().Contain("Application Name=TestApp");
    }

    [Fact]
    public void CreateConnectionString_EmptyPort_OmitsPortFromConnectionString()
    {
        // Arrange
        ConnectionModel model = new()
        {
            Host = "localhost",
            DatabaseName = "testdb",
            UserId = "user",
            Password = "pass",
            Port = ""
        };

        // Act
        string result = DbConnectionModel.CreateConnectionString(model, "TestApp");

        // Assert
        result.Should().Contain("Host=localhost");
        result.Should().NotContain("Port=");
    }

    [Fact]
    public void CreateConnectionString_InvalidPort_OmitsPortFromConnectionString()
    {
        // Arrange
        ConnectionModel model = new()
        {
            Host = "localhost",
            DatabaseName = "testdb",
            UserId = "user",
            Password = "pass",
            Port = "not-a-number"
        };

        // Act
        string result = DbConnectionModel.CreateConnectionString(model, "TestApp");

        // Assert
        result.Should().Contain("Host=localhost");
        result.Should().NotContain("Port=not-a-number");
    }

    [Fact]
    public void CreateConnectionString_ValidPort_IncludesPortInConnectionString()
    {
        // Arrange
        ConnectionModel model = new()
        {
            Host = "localhost",
            DatabaseName = "testdb",
            UserId = "user",
            Password = "pass",
            Port = "5433"
        };

        // Act
        string result = DbConnectionModel.CreateConnectionString(model, "TestApp");

        // Assert
        result.Should().Contain("Port=5433");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateConnectionString_NullOrEmptySslMode_UsesPrefer(string? sslMode)
    {
        // Arrange
        ConnectionModel model = new()
        {
            Host = "localhost",
            DatabaseName = "testdb",
            UserId = "user",
            Password = "pass",
            SslMode = sslMode
        };

        // Act
        string result = DbConnectionModel.CreateConnectionString(model, "TestApp");

        // Assert - Prefer is the default for null/empty (Npgsql uses "SSL Mode")
        result.Should().Contain("SSL Mode=Prefer");
    }

    [Theory]
    [InlineData("DISABLE")]
    [InlineData("disable")]
    [InlineData("Allow")]
    [InlineData("ALLOW")]
    [InlineData("PREFER")]
    [InlineData("REQUIRE")]
    [InlineData("VERIFYCA")]
    [InlineData("VERIFYFULL")]
    public void CreateConnectionString_ValidSslModes_ParsesCorrectly(string sslMode)
    {
        // Arrange
        ConnectionModel model = new()
        {
            Host = "localhost",
            DatabaseName = "testdb",
            UserId = "user",
            Password = "pass",
            SslMode = sslMode
        };

        // Act
        string result = DbConnectionModel.CreateConnectionString(model, "TestApp");

        // Assert
        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("SSL Mode");
    }

    [Fact]
    public void CreateConnectionString_InvalidSslMode_DefaultsToRequire()
    {
        // Arrange
        ConnectionModel model = new()
        {
            Host = "localhost",
            DatabaseName = "testdb",
            UserId = "user",
            Password = "pass",
            SslMode = "INVALID_MODE"
        };

        // Act
        string result = DbConnectionModel.CreateConnectionString(model, "TestApp");

        // Assert - invalid values default to Require (Npgsql uses "SSL Mode")
        result.Should().Contain("SSL Mode=Require");
    }

    [Fact]
    public void CreateConnectionString_CustomPoolSettings_UsesProvidedValues()
    {
        // Arrange
        ConnectionModel model = new()
        {
            Host = "localhost",
            DatabaseName = "testdb",
            UserId = "user",
            Password = "pass",
            MaxPoolSize = 50,
            MinPoolSize = 2,
            CommandTimeout = 60,
            Timeout = 30
        };

        // Act
        string result = DbConnectionModel.CreateConnectionString(model, "TestApp");

        // Assert
        result.Should().Contain("Maximum Pool Size=50");
        result.Should().Contain("Minimum Pool Size=2");
        result.Should().Contain("Command Timeout=60");
        result.Should().Contain("Timeout=30");
    }

    #endregion
}
