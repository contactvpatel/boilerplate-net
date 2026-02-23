using FluentAssertions;
using WebShop.Infrastructure.Helpers;
using WebShop.UnitTests.Common;
using Xunit;

namespace WebShop.UnitTests.Infrastructure.Helpers;

/// <summary>
/// Unit tests for SqlIdentifierValidator.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class SqlIdentifierValidatorTests
{
    #region Valid Identifier Tests

    [Theory]
    [InlineData("schema")]
    [InlineData("table_name")]
    [InlineData("column1")]
    [InlineData("id")]
    [InlineData("webshop")]
    public void Validate_ValidIdentifier_ReturnsValue(string value)
    {
        // Act
        string result = SqlIdentifierValidator.Validate(value, nameof(value));

        // Assert
        result.Should().Be(value);
    }

    [Fact]
    public void Validate_ValidIdentifierWithTrimQuotes_ReturnsTrimmedValue()
    {
        // Arrange
        const string value = "\"schema\"";

        // Act
        string result = SqlIdentifierValidator.Validate(value, nameof(value), trimQuotes: true);

        // Assert
        result.Should().Be("schema");
    }

    #endregion

    #region Null and Whitespace Tests

    [Fact]
    public void Validate_Null_ThrowsArgumentException()
    {
        // Act
        Action act = () => SqlIdentifierValidator.Validate(null!, "paramName");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("paramName")
            .WithMessage("*Identifier cannot be null or whitespace*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Validate_Whitespace_ThrowsArgumentException(string value)
    {
        // Act
        Action act = () => SqlIdentifierValidator.Validate(value, "paramName");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("paramName")
            .WithMessage("*Identifier cannot be null or whitespace*");
    }

    #endregion

    #region Invalid Characters (SQL Injection Prevention) Tests

    [Theory]
    [InlineData("schema;table")]
    [InlineData("table--comment")]
    [InlineData("column' OR '1'='1")]
    [InlineData("id; DROP TABLE users--")]
    [InlineData("name-with-dash")]
    [InlineData("col.umn")]
    [InlineData("col umn")]
    [InlineData("col[umn]")]
    public void Validate_InvalidCharacters_ThrowsArgumentException(string value)
    {
        // Act
        Action act = () => SqlIdentifierValidator.Validate(value, "paramName");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("paramName")
            .WithMessage("*contains invalid characters*")
            .WithMessage("*Only alphanumeric and underscore are allowed*");
    }

    [Fact]
    public void Validate_SqlInjectionAttempt_ThrowsArgumentException()
    {
        // Arrange - common SQL injection patterns
        const string value = "'; DELETE FROM customers; --";

        // Act
        Action act = () => SqlIdentifierValidator.Validate(value, "paramName");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("paramName");
    }

    #endregion

    #region TrimQuotes Tests

    [Fact]
    public void Validate_QuotedIdentifierWithTrimQuotesFalse_ThrowsArgumentException()
    {
        // Arrange - quoted identifier without trimQuotes contains invalid char (quote)
        const string value = "\"schema\"";

        // Act
        Action act = () => SqlIdentifierValidator.Validate(value, "paramName", trimQuotes: false);

        // Assert - double quote is not alphanumeric or underscore
        act.Should().Throw<ArgumentException>()
            .WithParameterName("paramName");
    }

    [Fact]
    public void Validate_EmptyAfterTrimQuotes_ThrowsArgumentException()
    {
        // Arrange - value becomes empty after trimming quotes
        const string value = "\"\"";

        // Act
        Action act = () => SqlIdentifierValidator.Validate(value, "paramName", trimQuotes: true);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("paramName")
            .WithMessage("*Identifier cannot be null or whitespace*");
    }

    #endregion

    #region ParamName Propagation

    [Fact]
    public void Validate_Exception_IncludesParamName()
    {
        // Act
        Action act = () => SqlIdentifierValidator.Validate("invalid-name", "schemaName");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithParameterName("schemaName");
    }

    #endregion
}
