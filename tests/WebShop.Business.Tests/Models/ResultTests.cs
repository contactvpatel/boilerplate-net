using FluentAssertions;
using WebShop.Business.Models;
using Xunit;

namespace WebShop.Business.Tests.Models;

/// <summary>
/// Unit tests for Result&lt;T&gt;.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class ResultTests
{
    [Fact]
    public void Success_WithValue_IsSuccessTrue()
    {
        Result<int> result = Result<int>.Success(42);
        result.IsSuccess.Should().BeTrue();
        result.IsNotFound.Should().BeFalse();
        result.IsFailure.Should().BeFalse();
        result.Value.Should().Be(42);
        result.ValueOrNull.Should().Be(42);
        result.ToNullable().Should().Be(42);
    }

    [Fact]
    public void NotFound_IsNotFoundTrue()
    {
        Result<string> result = Result<string>.NotFound();
        result.IsSuccess.Should().BeFalse();
        result.IsNotFound.Should().BeTrue();
        result.IsFailure.Should().BeFalse();
        result.ValueOrNull.Should().BeNull();
        result.ToNullable().Should().BeNull();
    }

    [Fact]
    public void NotFound_Value_ThrowsInvalidOperationException()
    {
        Result<int> result = Result<int>.NotFound();
        Action act = () => _ = result.Value;
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("Cannot access Value when result is NotFound.");
    }

    [Fact]
    public void Failure_WithMessage_IsFailureTrue()
    {
        Result<int> result = Result<int>.Failure("Duplicate email");
        result.IsSuccess.Should().BeFalse();
        result.IsNotFound.Should().BeFalse();
        result.IsFailure.Should().BeTrue();
        result.ErrorMessage.Should().Be("Duplicate email");
    }

    [Fact]
    public void Failure_NullMessage_ThrowsArgumentNullException()
    {
        Action act = () => Result<int>.Failure(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Failure_EmptyMessage_ThrowsArgumentException()
    {
        Action act = () => Result<int>.Failure(string.Empty);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Failure_WhitespaceMessage_ThrowsArgumentException()
    {
        Action act = () => Result<int>.Failure("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Match_TwoOutcomes_Success_CallsOnSuccess()
    {
        Result<int> result = Result<int>.Success(10);
        int value = result.Match(v => v * 2, () => -1);
        value.Should().Be(20);
    }

    [Fact]
    public void Match_TwoOutcomes_NotFound_CallsOnNotFound()
    {
        Result<int> result = Result<int>.NotFound();
        int value = result.Match(v => v * 2, () => -1);
        value.Should().Be(-1);
    }

    [Fact]
    public void Match_ThreeOutcomes_Success_CallsOnSuccess()
    {
        Result<int> result = Result<int>.Success(5);
        string value = result.Match(v => $"ok:{v}", () => "notfound", _ => "failure");
        value.Should().Be("ok:5");
    }

    [Fact]
    public void Match_ThreeOutcomes_NotFound_CallsOnNotFound()
    {
        Result<int> result = Result<int>.NotFound();
        string value = result.Match(v => $"ok:{v}", () => "notfound", _ => "failure");
        value.Should().Be("notfound");
    }

    [Fact]
    public void Match_ThreeOutcomes_Failure_CallsOnFailure()
    {
        Result<int> result = Result<int>.Failure("Duplicate");
        string value = result.Match(v => $"ok:{v}", () => "notfound", msg => $"err:{msg}");
        value.Should().Be("err:Duplicate");
    }
}
