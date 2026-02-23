namespace WebShop.UnitTests.Common;

/// <summary>
/// Test category constants for BAPS test tagging strategy.
/// Use with [Trait("Category", TestCategories.Unit)] for CI filtering.
/// </summary>
public static class TestCategories
{
    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string E2E = "E2E";
}
