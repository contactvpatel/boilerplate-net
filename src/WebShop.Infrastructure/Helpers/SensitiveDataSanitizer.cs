using System.Text.RegularExpressions;

namespace WebShop.Infrastructure.Helpers;

/// <summary>
/// Utility class for sanitizing sensitive data from logs and error messages.
/// </summary>
public static class SensitiveDataSanitizer
{
    private static readonly Regex TokenPattern = new(@"\b(?:Bearer\s+)?([A-Za-z0-9\-_.]{20,})\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PasswordPattern = new(@"(?:password|pwd|pass|secret|token|apikey|apisecret)\s*[:=]\s*([^\s""',;]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex CreditCardPattern = new(@"\b(?:\d{4}[-\s]?){3}\d{4}\b", RegexOptions.Compiled);
    private static readonly Regex SsnPattern = new(@"\b\d{3}-\d{2}-\d{4}\b", RegexOptions.Compiled);

    /// <summary>
    /// Sanitizes sensitive data from a string, replacing tokens, passwords, and other sensitive information.
    /// </summary>
    /// <param name="input">The input string that may contain sensitive data.</param>
    /// <returns>A sanitized string with sensitive data replaced with placeholders.</returns>
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input ?? string.Empty;
        }

        string sanitized = input;

        // Replace JWT tokens and bearer tokens
        sanitized = TokenPattern.Replace(sanitized, match =>
        {
            string token = match.Groups[1].Value;
            // Keep first 4 and last 4 characters, mask the rest
            if (token.Length > 8)
            {
                return $"{token[..4]}...{token[^4..]}";
            }
            return "***";
        });

        // Replace passwords and secrets
        sanitized = PasswordPattern.Replace(sanitized, match =>
        {
            string key = match.Groups[0].Value;
            return key.Split(':', '=')[0] + ": ***";
        });

        // Replace credit card numbers
        sanitized = CreditCardPattern.Replace(sanitized, "****-****-****-****");

        // Replace SSN
        sanitized = SsnPattern.Replace(sanitized, "***-**-****");

        return sanitized;
    }

    /// <summary>
    /// Sanitizes an exception message, removing sensitive data.
    /// </summary>
    /// <param name="exception">The exception to sanitize.</param>
    /// <returns>A sanitized exception message.</returns>
    public static string SanitizeException(Exception? exception)
    {
        if (exception == null)
        {
            return string.Empty;
        }

        string message = exception.Message;
        if (!string.IsNullOrWhiteSpace(exception.StackTrace))
        {
            message += $"\n{exception.StackTrace}";
        }

        return Sanitize(message);
    }
}

