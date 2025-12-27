namespace WebShop.Api.Models;

/// <summary>
/// Standardized API response model for all endpoints.
/// </summary>
/// <typeparam name="T">The type of data returned in the response.</typeparam>
public class Response<T>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Response{T}"/> class.
    /// </summary>
    public Response()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Response{T}"/> class with data.
    /// </summary>
    /// <param name="data">The response data.</param>
    public Response(T? data)
    {
        Succeeded = true;
        Message = string.Empty;
        Errors = null;
        Data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Response{T}"/> class with data and message.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <param name="message">The response message.</param>
    public Response(T? data, string message)
    {
        Succeeded = true;
        Message = message;
        Errors = null;
        Data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Response{T}"/> class with success status and message.
    /// </summary>
    /// <param name="succeeded">Indicates whether the operation succeeded.</param>
    /// <param name="message">The response message.</param>
    public Response(bool succeeded, string message)
    {
        Succeeded = succeeded;
        Message = message;
        Errors = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Response{T}"/> class with data, success status, and message.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <param name="succeeded">Indicates whether the operation succeeded.</param>
    /// <param name="message">The response message.</param>
    public Response(T? data, bool succeeded, string message)
    {
        Succeeded = succeeded;
        Message = message;
        Errors = null;
        Data = data;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Response{T}"/> class with data, success status, message, and errors.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <param name="succeeded">Indicates whether the operation succeeded.</param>
    /// <param name="message">The response message.</param>
    /// <param name="errors">List of errors if any.</param>
    public Response(T? data, bool succeeded, string message, List<ApiError> errors)
    {
        Succeeded = succeeded;
        Message = message;
        Errors = errors;
        Data = data;
    }

    /// <summary>
    /// Gets or sets a value indicating whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Gets or sets the response data.
    /// </summary>
    public T? Data { get; set; } = default;

    /// <summary>
    /// Gets or sets the response message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of errors, if any.
    /// </summary>
    public List<ApiError>? Errors { get; set; }

    /// <summary>
    /// Creates a successful response with data and message.
    /// </summary>
    /// <param name="data">The response data.</param>
    /// <param name="message">The response message.</param>
    /// <returns>A successful response.</returns>
    public static Response<T> Success(T? data, string message = "")
    {
        return new Response<T>(data, true, message);
    }

    /// <summary>
    /// Creates a failure response with message and optional errors.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errors">Optional list of errors.</param>
    /// <returns>A failure response.</returns>
    public static Response<T> Failure(string message, List<ApiError>? errors = null)
    {
        return new Response<T>(default, false, message, errors ?? new List<ApiError>());
    }

    /// <summary>
    /// Creates a not found response with message and optional error details.
    /// </summary>
    /// <param name="message">The error message.</param>
    /// <param name="errorDetails">Optional error details to include in the response.</param>
    /// <returns>A not found response.</returns>
    public static Response<T> NotFound(string message, params string[] errorDetails)
    {
        List<ApiError> errors = errorDetails.Length > 0
            ? errorDetails.Select(detail => new ApiError
            {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = (short)System.Net.HttpStatusCode.NotFound,
                Message = detail
            }).ToList()
            : new List<ApiError>
            {
                new ApiError
                {
                    ErrorId = Guid.NewGuid().ToString(),
                    StatusCode = (short)System.Net.HttpStatusCode.NotFound,
                    Message = message
                }
            };

        return new Response<T>(default, false, message, errors);
    }
}

