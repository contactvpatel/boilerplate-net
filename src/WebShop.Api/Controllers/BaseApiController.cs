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
    /// Handles paginated or non-paginated list responses. Returns all items when pagination is not requested,
    /// otherwise returns a paged result with metadata.
    /// </summary>
    /// <typeparam name="T">The item type.</typeparam>
    /// <param name="pagination">Pagination parameters.</param>
    /// <param name="getAllAsync">Delegate to fetch all items when not paginated.</param>
    /// <param name="getPagedAsync">Delegate to fetch paged items (page, pageSize, cancellationToken) returning (items, totalCount).</param>
    /// <param name="entityNamePlural">Entity name for success messages (e.g., "Products", "Customers").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An IActionResult with the appropriate response.</returns>
    protected async Task<IActionResult> GetPagedOrAllAsync<T>(
        PaginationQuery pagination,
        Func<CancellationToken, Task<IReadOnlyList<T>>> getAllAsync,
        Func<int, int, CancellationToken, Task<(IReadOnlyList<T> Items, int TotalCount)>> getPagedAsync,
        string entityNamePlural,
        CancellationToken cancellationToken)
    {
        if (!pagination.IsPaginated)
        {
            IReadOnlyList<T> all = await getAllAsync(cancellationToken);
            return Ok(Response<IReadOnlyList<T>>.Success(all, $"{entityNamePlural} retrieved successfully"));
        }

        (IReadOnlyList<T> items, int totalCount) = await getPagedAsync(pagination.Page, pagination.PageSize, cancellationToken);
        PagedResult<T> pagedResult = new(items, pagination.Page, pagination.PageSize, totalCount);

        return Ok(Response<PagedResult<T>>.Success(
            pagedResult,
            $"Retrieved page {pagination.Page} of {pagedResult.TotalPages} ({items.Count} of {totalCount} total {entityNamePlural.ToLowerInvariant()})"));
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
