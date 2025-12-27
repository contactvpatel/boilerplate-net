namespace WebShop.Api.Models;

/// <summary>
/// Represents an API error with error details.
/// </summary>
public class ApiError
{
    /// <summary>
    /// Gets or sets the unique error identifier.
    /// </summary>
    public string ErrorId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the HTTP status code associated with the error.
    /// </summary>
    public short StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

