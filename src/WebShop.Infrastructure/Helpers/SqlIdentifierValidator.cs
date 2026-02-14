namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Validates SQL identifiers (schema, table, column names) to prevent SQL injection.
/// Use when identifiers are interpolated into queries. Values must be parameterized separately.
/// </summary>
public static class SqlIdentifierValidator
{
    /// <summary>
    /// Validates that an identifier contains only safe characters (alphanumeric and underscore).
    /// Optionally trims surrounding double quotes (PostgreSQL identifier quoting).
    /// </summary>
    /// <param name="value">The identifier to validate.</param>
    /// <param name="paramName">Parameter name for exception messages.</param>
    /// <param name="trimQuotes">If true, trims surrounding double quotes before validation.</param>
    /// <returns>The validated identifier (cleaned if trimQuotes is true).</returns>
    /// <exception cref="ArgumentException">Thrown when the identifier is null, whitespace, or contains invalid characters.</exception>
    public static string Validate(string value, string paramName, bool trimQuotes = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Identifier cannot be null or whitespace.", paramName);
        }

        string cleaned = trimQuotes ? value.Trim('"') : value;
        if (cleaned.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
        {
            throw new ArgumentException(
                $"Identifier '{value}' contains invalid characters. Only alphanumeric and underscore are allowed.",
                paramName);
        }

        return cleaned;
    }
}
