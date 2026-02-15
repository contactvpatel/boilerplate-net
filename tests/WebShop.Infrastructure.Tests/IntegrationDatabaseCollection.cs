using Xunit;

namespace WebShop.Infrastructure.Tests;

/// <summary>
/// Collection definition for repository tests that use the integration database.
/// Tests in this collection run sequentially to avoid deadlocks and data races.
/// </summary>
[CollectionDefinition("IntegrationDatabase")]
public class IntegrationDatabaseCollection : ICollectionFixture<Helpers.TestDatabaseFixture>
{
}
