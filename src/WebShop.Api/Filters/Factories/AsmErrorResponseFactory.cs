using System.Net;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;

namespace WebShop.Api.Filters.Factories;

/// <summary>
/// Factory for creating ASM authorization error responses.
/// </summary>
public class AsmErrorResponseFactory : IAsmErrorResponseFactory
{
    public IActionResult CreateUnauthorizedResponse(string message)
    {
        return CreateErrorResponse(HttpStatusCode.Unauthorized, message);
    }

    public IActionResult CreateForbiddenResponse(string message)
    {
        return CreateErrorResponse(HttpStatusCode.Forbidden, message);
    }

    public IActionResult CreateInternalServerErrorResponse(string message)
    {
        return CreateErrorResponse(HttpStatusCode.InternalServerError, message);
    }

    /// <summary>
    /// Creates an error response with the specified status code and message.
    /// </summary>
    private static IActionResult CreateErrorResponse(HttpStatusCode statusCode, string message)
    {
        List<ApiError> errors =
        [
            new() {
                ErrorId = Guid.NewGuid().ToString(),
                StatusCode = (short)statusCode,
                Message = message
            }
        ];

        Response<object> response = new(null, false, message, errors);

        return statusCode switch
        {
            HttpStatusCode.Unauthorized => new UnauthorizedObjectResult(response),
            HttpStatusCode.Forbidden => new ObjectResult(response) { StatusCode = (int)HttpStatusCode.Forbidden },
            HttpStatusCode.InternalServerError => new ObjectResult(response) { StatusCode = (int)HttpStatusCode.InternalServerError },
            _ => new BadRequestObjectResult(response)
        };
    }
}
