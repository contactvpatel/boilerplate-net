namespace WebShop.Api.Models;

/// <summary>
/// Options for configuring exception handling middleware behavior.
/// </summary>
public class ExceptionHandlingOptions
{
    /// <summary>
    /// Optional action to add additional details to the error response.
    /// </summary>
    public Action<HttpContext, Exception, Response<ApiError>>? AddResponseDetails { get; set; }

    /// <summary>
    /// Optional function to determine the log level based on the exception type.
    /// </summary>
    public Func<Exception, LogLevel>? DetermineLogLevel { get; set; }
}

