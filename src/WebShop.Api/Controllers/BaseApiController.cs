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
    /// Builds a list of <see cref="ApiError"/> for the given status code, using either the message (when no details) or one error per detail.
    /// </summary>
    private static List<ApiError> BuildErrors(HttpStatusCode statusCode, string message, string[] errorDetails)
    {
        short code = (short)statusCode;
        if (errorDetails.Length > 0)
        {
            return errorDetails.Select(detail => new ApiError
            {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = code,
                Message = detail
            }).ToList();
        }

        return
        [
            new ApiError
            {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = code,
                Message = message
            }
        ];
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
        return BadRequest(Response<T>.Failure(message, BuildErrors(HttpStatusCode.BadRequest, message, errorDetails)));
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
        return Unauthorized(Response<T>.Failure(message, BuildErrors(HttpStatusCode.Unauthorized, message, errorDetails)));
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
        return StatusCode((int)HttpStatusCode.InternalServerError, Response<T>.Failure(message, BuildErrors(HttpStatusCode.InternalServerError, message, errorDetails)));
    }

    /// <summary>
    /// Returns a standardized NotFound response for an entity identified by name and identifier.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="entityName">The name of the entity (e.g., "Customer", "Product").</param>
    /// <param name="identifier">The identifier that was not found (e.g., "ID", "email").</param>
    /// <param name="identifierValue">The value of the identifier.</param>
    /// <returns>A NotFoundObjectResult with standardized error response.</returns>
    protected NotFoundObjectResult HandleNotFound<T>(string entityName, string identifier, object identifierValue)
    {
        return NotFound(Response<T>.NotFound($"{entityName} not found", $"{entityName} with {identifier} {identifierValue} not found."));
    }
}
