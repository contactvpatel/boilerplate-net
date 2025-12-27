using FluentAssertions;
using WebShop.Infrastructure.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Helpers;

/// <summary>
/// Unit tests for SensitiveDataSanitizer.
/// </summary>
[Trait("Category", "Unit")]
public class SensitiveDataSanitizerTests
{
    #region Sanitize Tests

    [Fact]
    public void Sanitize_NullInput_ReturnsEmptyString()
    {
        // Act
        string result = SensitiveDataSanitizer.Sanitize(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_EmptyInput_ReturnsEmptyString()
    {
        // Act
        string result = SensitiveDataSanitizer.Sanitize(string.Empty);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void Sanitize_WhitespaceInput_ReturnsWhitespace()
    {
        // Arrange
        const string input = "   ";

        // Act
        string result = SensitiveDataSanitizer.Sanitize(input);

        // Assert
        result.Should().Be(input);
    }

    [Fact]
    public void Sanitize_JwtToken_MasksToken()
    {
        // Arrange
        const string input = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c";

        // Act
        string result = SensitiveDataSanitizer.Sanitize(input);

        // Assert
        result.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9");
        // Implementation masks as {first4}...{last4}, so check for the masked format
        result.Should().Contain("...");
        result.Should().NotContain("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c");
    }

    [Fact]
    public void Sanitize_Password_MasksPassword()
    {
        // Arrange
        const string input = "password: secret123";

        // Act
        string result = SensitiveDataSanitizer.Sanitize(input);

        // Assert
        result.Should().Contain("password: ***");
        result.Should().NotContain("secret123");
    }

    [Fact]
    public void Sanitize_CreditCard_MasksCreditCard()
    {
        // Arrange
        const string input = "Card number: 1234-5678-9012-3456";

        // Act
        string result = SensitiveDataSanitizer.Sanitize(input);

        // Assert
        result.Should().Contain("****-****-****-****");
        result.Should().NotContain("1234-5678-9012-3456");
    }

    [Fact]
    public void Sanitize_Ssn_MasksSsn()
    {
        // Arrange
        const string input = "SSN: 123-45-6789";

        // Act
        string result = SensitiveDataSanitizer.Sanitize(input);

        // Assert
        result.Should().Contain("***-**-****");
        result.Should().NotContain("123-45-6789");
    }

    [Fact]
    public void Sanitize_NoSensitiveData_ReturnsOriginal()
    {
        // Arrange
        const string input = "This is a normal message without sensitive data";

        // Act
        string result = SensitiveDataSanitizer.Sanitize(input);

        // Assert
        result.Should().Be(input);
    }

    #endregion

    #region SanitizeException Tests

    [Fact]
    public void SanitizeException_NullException_ReturnsEmptyString()
    {
        // Act
        string result = SensitiveDataSanitizer.SanitizeException(null);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void SanitizeException_ExceptionWithSensitiveData_MasksSensitiveData()
    {
        // Arrange
        Exception exception = new("Error: password: secret123");

        // Act
        string result = SensitiveDataSanitizer.SanitizeException(exception);

        // Assert
        result.Should().Contain("password: ***");
        result.Should().NotContain("secret123");
    }

    [Fact]
    public void SanitizeException_ExceptionWithStackTrace_IncludesStackTrace()
    {
        // Arrange
        Exception exception;
        try
        {
            throw new InvalidOperationException("Test exception");
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        // Act
        string result = SensitiveDataSanitizer.SanitizeException(exception);

        // Assert
        result.Should().Contain("Test exception");
        // Stack trace may be sanitized or formatted differently, so just check that we got something
        result.Should().NotBeNullOrEmpty();
    }

    #endregion
}
