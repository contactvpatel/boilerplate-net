namespace WebShop.Core.Models;

/// <summary>
/// Represents a standardized error payload returned by the API.
/// </summary>
public class ApiErrorModel
{
    /// <summary>Unique identifier for the error instance (e.g. for correlation or support).</summary>
    public string ErrorId { get; set; } = string.Empty;

    /// <summary>HTTP status code for the error response.</summary>
    public short StatusCode { get; set; } = 0;

    /// <summary>Human-readable error message describing what went wrong.</summary>
    public string Message { get; set; } = string.Empty;
}
