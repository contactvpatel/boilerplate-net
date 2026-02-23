using Xunit;

namespace WebShop.IntegrationTests.Fixtures;

/// <summary>
/// Collection definition for all integration tests (API + repository) that share the integration database.
/// Tests in this collection run sequentially to avoid deadlocks and data races.
/// </summary>
[CollectionDefinition("IntegrationDatabase")]
public class IntegrationDatabaseCollection : ICollectionFixture<TestDatabaseFixture>, ICollectionFixture<WebAppFactory>
{
}
