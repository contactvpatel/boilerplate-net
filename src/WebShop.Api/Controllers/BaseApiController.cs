using System.Net;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;

namespace WebShop.Api.Controllers;

/// <summary>
/// Base controller class providing common functionality for all API controllers.
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// Creates a standardized NotFound response.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="message">The error message.</param>
    /// <param name="errorDetails">Optional additional error details.</param>
    /// <returns>A NotFoundObjectResult with standardized error response.</returns>
    protected NotFoundObjectResult NotFoundResponse<T>(string message, params string[] errorDetails)
    {
        return NotFound(Response<T>.NotFound(message, errorDetails));
    }

    /// <summary>
    /// Creates a standardized BadRequest response.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="message">The error message.</param>
    /// <param name="errorDetails">Optional additional error details.</param>
    /// <returns>A BadRequestObjectResult with standardized error response.</returns>
    protected BadRequestObjectResult BadRequestResponse<T>(string message, params string[] errorDetails)
    {
        List<ApiError> errors = errorDetails.Length > 0
            ? errorDetails.Select(detail => new ApiError
            {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = (short)HttpStatusCode.BadRequest,
                Message = detail
            }).ToList()
            : [new ApiError
            {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = (short)HttpStatusCode.BadRequest,
                Message = message
            }];

        return BadRequest(Response<T>.Failure(message, errors));
    }

    /// <summary>
    /// Creates a standardized Unauthorized response.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="message">The error message.</param>
    /// <param name="errorDetails">Optional additional error details.</param>
    /// <returns>An UnauthorizedObjectResult with standardized error response.</returns>
    protected UnauthorizedObjectResult UnauthorizedResponse<T>(string message, params string[] errorDetails)
    {
        List<ApiError> errors = errorDetails.Length > 0
            ? errorDetails.Select(detail => new ApiError
            {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = (short)HttpStatusCode.Unauthorized,
                Message = detail
            }).ToList()
            : [new ApiError
            {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = (short)HttpStatusCode.Unauthorized,
                Message = message
            }];

        return Unauthorized(Response<T>.Failure(message, errors));
    }

    /// <summary>
    /// Creates a standardized InternalServerError response.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="message">The error message.</param>
    /// <param name="errorDetails">Optional additional error details.</param>
    /// <returns>A StatusCodeResult with standardized error response.</returns>
    protected ObjectResult InternalServerErrorResponse<T>(string message, params string[] errorDetails)
    {
        List<ApiError> errors = errorDetails.Length > 0
            ? errorDetails.Select(detail => new ApiError
            {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = (short)HttpStatusCode.InternalServerError,
                Message = detail
            }).ToList()
            : [new ApiError
            {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = (short)HttpStatusCode.InternalServerError,
                Message = message
            }];

        return StatusCode((int)HttpStatusCode.InternalServerError, Response<T>.Failure(message, errors));
    }

    /// <summary>
    /// Handles a null result by returning a NotFound response.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="entityName">The name of the entity (e.g., "Customer", "Product").</param>
    /// <param name="identifier">The identifier that was not found (e.g., ID, email).</param>
    /// <param name="identifierValue">The value of the identifier.</param>
    /// <returns>A NotFoundObjectResult if entity is null, otherwise null.</returns>
    protected NotFoundObjectResult? HandleNotFound<T>(string entityName, string identifier, object identifierValue)
    {
        return NotFoundResponse<T>($"{entityName} not found", $"{entityName} with {identifier} {identifierValue} not found.");
    }
}
