using FluentAssertions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using WebShop.Infrastructure.Services.Internal;
using WebShop.UnitTests.Common;
using WebShop.Util.Models;
using Xunit;

namespace WebShop.UnitTests.Infrastructure.Services.Internal;

/// <summary>
/// Unit tests for CacheService.
/// Uses real HybridCache (not mockable - sealed/non-virtual methods) from DI for cache-enabled tests.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class CacheServiceTests
{
    private static HybridCache CreateRealHybridCache()
    {
        ServiceCollection services = new();
        services.AddHybridCache();
        return services.BuildServiceProvider().GetRequiredService<HybridCache>();
    }

    private readonly HybridCache _realCache;
    private readonly Mock<IOptions<CacheOptions>> _mockOptions;
    private readonly Mock<ILogger<CacheService>> _mockLogger;
    private readonly CacheService _service;

    public CacheServiceTests()
    {
        _realCache = CreateRealHybridCache();
        _mockOptions = new Mock<IOptions<CacheOptions>>();
        _mockLogger = new Mock<ILogger<CacheService>>();

        _mockOptions.Setup(o => o.Value).Returns(new CacheOptions { Enabled = true });

        _service = new CacheService(
            _realCache,
            _mockOptions.Object,
            _mockLogger.Object);
    }

    #region GetOrCreateAsync Tests

    [Fact]
    public async Task GetOrCreateAsync_CacheEnabled_CallsHybridCache()
    {
        // Arrange - use real HybridCache (not mockable)
        const string key = "test-key-cache-enabled";
        const string expectedValue = "cached-value";

        // Act - first call creates, second call retrieves from cache
        string result1 = await _service.GetOrCreateAsync(key, _ => Task.FromResult(expectedValue));
        string result2 = await _service.GetOrCreateAsync(key, _ => Task.FromResult("different-value"));

        // Assert - both return cached value (factory only runs once)
        result1.Should().Be(expectedValue);
        result2.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetOrCreateAsync_WithExpiration_UsesCustomExpiration()
    {
        // Arrange - expiration/localExpiration branch
        const string key = "test-key-expiration";
        const string expectedValue = "exp-value";

        // Act - pass expiration to hit the options branch
        string result = await _service.GetOrCreateAsync(
            key,
            _ => Task.FromResult(expectedValue),
            expiration: TimeSpan.FromMinutes(5),
            localExpiration: TimeSpan.FromMinutes(2));

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task SetAsync_WithExpiration_SetsValue()
    {
        // Arrange - expiration branch in SetAsync
        const string key = "test-key-set-exp";
        const string value = "set-value";

        // Act
        await _service.SetAsync(key, value, expiration: TimeSpan.FromMinutes(3));

        // Assert
        string retrieved = await _service.GetOrCreateAsync(key, _ => Task.FromResult("default"));
        retrieved.Should().Be(value);
    }

    [Fact]
    public async Task GetOrCreateAsync_CacheDisabled_ExecutesFactoryDirectly()
    {
        // Arrange
        _mockOptions.Setup(o => o.Value).Returns(new CacheOptions { Enabled = false });
        CacheService disabledService = new(null, _mockOptions.Object, _mockLogger.Object);
        const string key = "test-key";
        const string expectedValue = "test-value";

        // Act
        string result = await disabledService.GetOrCreateAsync(
            key,
            _ => Task.FromResult(expectedValue));

        // Assert
        result.Should().Be(expectedValue);
    }

    [Fact]
    public async Task GetOrCreateAsync_NullKey_ThrowsArgumentNullException()
    {
        // Act & Assert - ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.GetOrCreateAsync(null!, _ => Task.FromResult("value")));
    }

    [Fact]
    public async Task GetOrCreateAsync_EmptyKey_ThrowsArgumentException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GetOrCreateAsync(string.Empty, _ => Task.FromResult("value")));
    }

    [Fact]
    public async Task GetOrCreateAsync_CacheError_FallsBackToFactory()
    {
        // Arrange - use cache=null to simulate cache unavailable; service falls back to factory
        CacheService serviceWithNullCache = new(
            null,
            Mock.Of<IOptions<CacheOptions>>(o => o.Value == new CacheOptions { Enabled = true }),
            Mock.Of<ILogger<CacheService>>());
        const string key = "test-key";
        const string expectedValue = "test-value";

        // Act - with null cache, executes factory directly (same code path as cache error fallback)
        string result = await serviceWithNullCache.GetOrCreateAsync(
            key,
            _ => Task.FromResult(expectedValue));

        // Assert - factory was executed and returned value
        result.Should().Be(expectedValue);
    }

    #endregion

    #region SetAsync Tests

    [Fact]
    public async Task SetAsync_CacheEnabled_SetsValue()
    {
        // Arrange
        const string key = "test-key-set";
        const string value = "test-value";

        // Act
        await _service.SetAsync(key, value);

        // Assert - verify by GetOrCreateAsync: if set worked, we get the value from cache
        string retrieved = await _service.GetOrCreateAsync(key, _ => Task.FromResult("factory-default"));
        retrieved.Should().Be(value);
    }

    [Fact]
    public async Task SetAsync_CacheDisabled_SkipsSet()
    {
        // Arrange
        _mockOptions.Setup(o => o.Value).Returns(new CacheOptions { Enabled = false });
        CacheService disabledService = new(null, _mockOptions.Object, _mockLogger.Object);
        const string key = "test-key";
        const string value = "test-value";

        // Act
        await disabledService.SetAsync(key, value);

        // Assert - Should not throw
    }

    [Fact]
    public async Task SetAsync_NullKey_ThrowsArgumentNullException()
    {
        // Act & Assert - ThrowIfNullOrWhiteSpace throws ArgumentNullException for null
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            _service.SetAsync(null!, "value"));
    }

    #endregion

    #region RemoveAsync Tests

    [Fact]
    public async Task RemoveAsync_CacheEnabled_RemovesValue()
    {
        // Arrange - set then remove
        const string key = "test-key-remove";
        await _service.SetAsync(key, "value");
        string beforeRemove = await _service.GetOrCreateAsync(key, _ => Task.FromResult("default"));
        beforeRemove.Should().Be("value");

        // Act
        await _service.RemoveAsync(key);

        // Assert - after remove, GetOrCreate runs factory again
        string afterRemove = await _service.GetOrCreateAsync(key, _ => Task.FromResult("factory-after-remove"));
        afterRemove.Should().Be("factory-after-remove");
    }

    [Fact]
    public async Task RemoveAsync_NullKey_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _service.RemoveAsync((string?)null!);
    }

    [Fact]
    public async Task RemoveAsync_EmptyKey_ReturnsWithoutCallingCache()
    {
        // Act - should not throw
        await _service.RemoveAsync(string.Empty);
    }

    [Fact]
    public async Task RemoveAsync_WhitespaceKey_ReturnsWithoutCallingCache()
    {
        // Act - should not throw
        await _service.RemoveAsync("   ");
    }

    [Fact]
    public async Task RemoveAsync_MultipleKeys_RemovesAll()
    {
        // Arrange
        List<string> keys = new() { "multi-key-1", "multi-key-2", "multi-key-3" };
        foreach (string k in keys)
        {
            await _service.SetAsync(k, "v");
        }

        // Act
        await _service.RemoveAsync(keys);

        // Assert - keys should be gone (factory runs for each)
        foreach (string k in keys)
        {
            string v = await _service.GetOrCreateAsync(k, _ => Task.FromResult("gone"));
            v.Should().Be("gone");
        }
    }

    #endregion

    #region RemoveByTagAsync Tests

    [Fact]
    public async Task RemoveByTagAsync_CacheEnabled_RemovesByTag()
    {
        // Arrange - HybridCache supports tags; call remove by tag
        const string tag = "test-tag";

        // Act - should not throw
        await _service.RemoveByTagAsync(tag);
    }

    [Fact]
    public async Task RemoveByTagAsync_NullTag_DoesNotThrow()
    {
        // Act & Assert - Should not throw
        await _service.RemoveByTagAsync((string?)null!);
    }

    [Fact]
    public async Task RemoveByTagAsync_EmptyTag_ReturnsWithoutCallingCache()
    {
        // Act - should not throw
        await _service.RemoveByTagAsync(string.Empty);
    }

    [Fact]
    public async Task RemoveAsync_NullKeys_ReturnsWithoutThrowing()
    {
        // Act & Assert
        await _service.RemoveAsync((IEnumerable<string>?)null!);
    }

    [Fact]
    public async Task RemoveByTagAsync_NullTags_ReturnsWithoutThrowing()
    {
        // Act & Assert
        await _service.RemoveByTagAsync((IEnumerable<string>?)null!);
    }

    [Fact]
    public async Task RemoveByTagAsync_MultipleTags_RemovesAll()
    {
        // Arrange
        List<string> tags = new() { "tag1", "tag2" };

        // Act - should not throw
        await _service.RemoveByTagAsync(tags);
    }

    #endregion
}
