using FluentAssertions;
using WebShop.Business.Helpers;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.Business.Helpers;

/// <summary>
/// Unit tests for PartialUpdateHelper.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class PartialUpdateHelperTests
{
    #region ApplyIfChanged (string) Tests

    [Fact]
    public void ApplyIfChanged_String_PatchValueNull_ReturnsFalse()
    {
        string? current = "original";
        string? captured = null;

        bool result = PartialUpdateHelper.ApplyIfChanged(current, null, v => captured = v);

        result.Should().BeFalse();
        captured.Should().BeNull();
    }

    [Fact]
    public void ApplyIfChanged_String_SameValue_ReturnsFalse()
    {
        string current = "same";
        string? captured = null;

        bool result = PartialUpdateHelper.ApplyIfChanged(current, "same", v => captured = v);

        result.Should().BeFalse();
        captured.Should().BeNull();
    }

    [Fact]
    public void ApplyIfChanged_String_DifferentValue_ReturnsTrueAndCallsSetter()
    {
        string current = "original";
        string? captured = null;

        bool result = PartialUpdateHelper.ApplyIfChanged(current, "updated", v => captured = v);

        result.Should().BeTrue();
        captured.Should().Be("updated");
    }

    [Fact]
    public void ApplyIfChanged_String_CurrentNull_PatchValueProvided_ReturnsTrue()
    {
        string? current = null;
        string? captured = null;

        bool result = PartialUpdateHelper.ApplyIfChanged(current, "new", v => captured = v);

        result.Should().BeTrue();
        captured.Should().Be("new");
    }

    #endregion

    #region ApplyIfChanged (value type) Tests

    [Fact]
    public void ApplyIfChanged_ValueType_PatchValueNull_ReturnsFalse()
    {
        int? current = 10;
        int captured = 0;

        bool result = PartialUpdateHelper.ApplyIfChanged(current, null, v => captured = v);

        result.Should().BeFalse();
        captured.Should().Be(0);
    }

    [Fact]
    public void ApplyIfChanged_ValueType_SameValue_ReturnsFalse()
    {
        int? current = 42;
        int captured = 0;

        bool result = PartialUpdateHelper.ApplyIfChanged(current, 42, v => captured = v);

        result.Should().BeFalse();
        captured.Should().Be(0);
    }

    [Fact]
    public void ApplyIfChanged_ValueType_DifferentValue_ReturnsTrueAndCallsSetter()
    {
        int? current = 10;
        int captured = 0;

        bool result = PartialUpdateHelper.ApplyIfChanged(current, 20, v => captured = v);

        result.Should().BeTrue();
        captured.Should().Be(20);
    }

    [Fact]
    public void ApplyIfChanged_ValueType_CurrentNull_PatchValueProvided_ReturnsTrue()
    {
        int? current = null;
        int captured = 0;

        bool result = PartialUpdateHelper.ApplyIfChanged(current, 99, v => captured = v);

        result.Should().BeTrue();
        captured.Should().Be(99);
    }

    #endregion
}
