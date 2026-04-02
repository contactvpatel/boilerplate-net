using DbUp.Engine.Output;
using Microsoft.Extensions.Logging;

namespace WebShop.Infrastructure.DbUp.Core;

public class DbUpLoggerExtension(ILogger logger) : IUpgradeLog
{
    private readonly ILogger _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private static string SafeFormat(string format, object[] args)
    {
        if (args is null || args.Length == 0)
            return format;
        try
        {
            return string.Format(format, args);
        }
        catch (FormatException)
        {
            // format may be an exception/SQL message containing { or } - log as-is and append args
            return format + (args.Length > 0 ? " " + string.Join(" ", args) : "");
        }
    }

    public void LogTrace(string format, params object[] args)
    {
        _logger.LogTrace(SafeFormat(format, args));
    }

    public void LogDebug(string format, params object[] args)
    {
        _logger.LogDebug(SafeFormat(format, args));
    }

    public void LogInformation(string format, params object[] args)
    {
        _logger.LogInformation(SafeFormat(format, args));
    }

    public void LogWarning(string format, params object[] args)
    {
        _logger.LogWarning(SafeFormat(format, args));
    }

    public void LogError(string format, params object[] args)
    {
        _logger.LogError(SafeFormat(format, args));
    }

    public void LogError(Exception ex, string format, params object[] args)
    {
        _logger.LogError(ex, SafeFormat(format, args));
    }
}