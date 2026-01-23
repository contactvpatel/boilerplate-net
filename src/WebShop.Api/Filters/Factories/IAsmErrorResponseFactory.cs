using Microsoft.AspNetCore.Mvc;

namespace WebShop.Api.Filters.Factories;

/// <summary>
/// Interface for creating ASM error responses.
/// </summary>
public interface IAsmErrorResponseFactory
{
    /// <summary>
    /// Creates an unauthorized (401) response.
    /// </summary>
    IActionResult CreateUnauthorizedResponse(string message);

    /// <summary>
    /// Creates a forbidden (403) response.
    /// </summary>
    IActionResult CreateForbiddenResponse(string message);

    /// <summary>
    /// Creates an internal server error (500) response.
    /// </summary>
    IActionResult CreateInternalServerErrorResponse(string message);
}
