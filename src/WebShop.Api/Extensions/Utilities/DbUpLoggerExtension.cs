using DbUp.Engine.Output;

namespace WebShop.Api.Extensions.Utilities;

/// <summary>
/// DbUp logger extension that integrates with ASP.NET Core ILogger using structured logging.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="DbUpLoggerExtension"/> class.
/// </remarks>
/// <param name="logger">The logger instance.</param>
public class DbUpLoggerExtension(ILogger logger) : IUpgradeLog
{

    /// <inheritdoc />
    public void LogTrace(string format, params object[] args)
    {
        if (args.Length == 0)
        {
            logger.LogTrace(format);
        }
        else
        {
            // Pass format string and args to ILogger for structured logging
            // ILogger will handle formatting and preserve structured properties if format uses named placeholders
            logger.LogTrace(format, args);
        }
    }

    /// <inheritdoc />
    public void LogDebug(string format, params object[] args)
    {
        if (args.Length == 0)
        {
            logger.LogDebug(format);
        }
        else
        {
            // Pass format string and args to ILogger for structured logging
            // ILogger will handle formatting and preserve structured properties if format uses named placeholders
            logger.LogDebug(format, args);
        }
    }

    /// <inheritdoc />
    public void LogInformation(string format, params object[] args)
    {
        if (args.Length == 0)
        {
            logger.LogInformation(format);
        }
        else
        {
            // Pass format string and args to ILogger for structured logging
            // ILogger will handle formatting and preserve structured properties if format uses named placeholders
            logger.LogInformation(format, args);
        }
    }

    /// <inheritdoc />
    public void LogWarning(string format, params object[] args)
    {
        if (args.Length == 0)
        {
            logger.LogWarning(format);
        }
        else
        {
            // Pass format string and args to ILogger for structured logging
            // ILogger will handle formatting and preserve structured properties if format uses named placeholders
            logger.LogWarning(format, args);
        }
    }

    /// <inheritdoc />
    public void LogError(string format, params object[] args)
    {
        if (args.Length == 0)
        {
            logger.LogError(format);
        }
        else
        {
            // Pass format string and args to ILogger for structured logging
            // ILogger will handle formatting and preserve structured properties if format uses named placeholders
            logger.LogError(format, args);
        }
    }

    /// <inheritdoc />
    public void LogError(Exception ex, string format, params object[] args)
    {
        if (args.Length == 0)
        {
            logger.LogError(ex, format);
        }
        else
        {
            // Pass format string, args, and exception to ILogger for structured logging
            // ILogger will handle formatting and preserve structured properties if format uses named placeholders
            logger.LogError(ex, format, args);
        }
    }
}
