using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.HostedServices;
using WebShop.Infrastructure.Interfaces;
using Xunit;

namespace WebShop.Api.Tests.HostedServices;

/// <summary>
/// Unit tests for DatabaseConnectionValidationHostedService.
/// </summary>
[Trait("Category", "Unit")]
public class DatabaseConnectionValidationHostedServiceTests
{
    [Fact]
    public void Constructor_AcceptsDependencies()
    {
        // Arrange & Act
        Mock<IDapperConnectionFactory> mockConnectionFactory = new();
        Mock<ILogger<DatabaseConnectionValidationHostedService>> mockLogger = new();
        DatabaseConnectionValidationHostedService service = new(
            mockConnectionFactory.Object,
            mockLogger.Object);

        // Assert
        service.Should().NotBeNull();
    }
}
