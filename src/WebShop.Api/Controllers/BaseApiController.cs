using System.Net;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.Models;

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
    /// Returns a standardized 200 OK response with success data.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="data">The response data.</param>
    /// <param name="message">The success message.</param>
    /// <returns>An OkObjectResult with standardized success response.</returns>
    protected OkObjectResult OkResponse<T>(T? data, string message = "")
    {
        return Ok(Response<T>.Success(data, message));
    }

    /// <summary>
    /// Returns a standardized 201 Created response with Location header pointing to the resource.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="actionName">The name of the action that returns the created resource (e.g., nameof(GetById)).</param>
    /// <param name="routeValues">Route values for the Location URL (e.g., new { id = entity.Id }).</param>
    /// <param name="data">The created resource.</param>
    /// <param name="message">The success message.</param>
    /// <returns>A CreatedAtActionResult with standardized success response.</returns>
    protected CreatedAtActionResult CreatedAtActionResponse<T>(string actionName, object routeValues, T data, string message)
    {
        return CreatedAtAction(actionName, routeValues, Response<T>.Success(data, message));
    }

    /// <summary>
    /// Returns a standardized 201 Created response with body (no Location header).
    /// Use for batch or multi-resource creation where a single Location is not applicable.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="data">The created resource(s).</param>
    /// <param name="message">The success message.</param>
    /// <returns>An ObjectResult with 201 status and standardized success response.</returns>
    protected ObjectResult CreatedResponse<T>(T data, string message)
    {
        return StatusCode(StatusCodes.Status201Created, Response<T>.Success(data, message));
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
            return OkResponse(all, $"{entityNamePlural} retrieved successfully");
        }

        (IReadOnlyList<T> items, int totalCount) = await getPagedAsync(pagination.Page, pagination.PageSize, cancellationToken);
        PagedResult<T> pagedResult = new(items, pagination.Page, pagination.PageSize, totalCount);

        return OkResponse(
            pagedResult,
            $"Retrieved page {pagination.Page} of {pagedResult.TotalPages} ({items.Count} of {totalCount} total {entityNamePlural.ToLowerInvariant()})");
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

    /// <summary>
    /// Common pattern for GetById: fetches entity by ID, returns 404 if not found, otherwise 200 with success response.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <param name="getByIdAsync">Delegate to fetch the entity by ID (returns Result&lt;T&gt;).</param>
    /// <param name="entityName">The name of the entity (e.g., "Customer", "Address").</param>
    /// <param name="successMessage">Message for successful retrieval.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="onNotFound">Optional callback when entity is not found (e.g., for logging).</param>
    /// <returns>404 if not found, 200 with data if found.</returns>
    protected async Task<ActionResult<Response<T>>> GetByIdOrNotFoundAsync<T>(
        int id,
        Func<int, CancellationToken, Task<Result<T>>> getByIdAsync,
        string entityName,
        string successMessage,
        CancellationToken cancellationToken,
        Action<int>? onNotFound = null)
    {
        Result<T> result = await getByIdAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            onNotFound?.Invoke(id);
            return HandleNotFound<T>(entityName, "ID", id);
        }

        return OkResponse(result.Value, successMessage);
    }

    /// <summary>
    /// Common pattern for GetByProperty (e.g., email, name): fetches entity, returns 404 if not found, otherwise 200.
    /// </summary>
    protected async Task<ActionResult<Response<T>>> GetByPropertyOrNotFoundAsync<T>(
        Func<CancellationToken, Task<Result<T>>> getAsync,
        string entityName,
        string identifier,
        object identifierValue,
        string successMessage,
        CancellationToken cancellationToken,
        Action? onNotFound = null)
    {
        Result<T> result = await getAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            onNotFound?.Invoke();
            return HandleNotFound<T>(entityName, identifier, identifierValue);
        }

        return OkResponse(result.Value, successMessage);
    }

    /// <summary>
    /// Common pattern for POST (create): executes create, returns 201 Created with Location header.
    /// </summary>
    /// <typeparam name="T">The response data type (must have Id for route values).</typeparam>
    /// <param name="createAsync">Delegate to create the resource.</param>
    /// <param name="actionName">Name of the GetById action for the Location header.</param>
    /// <param name="getRouteValues">Extracts route values from the created resource (e.g., r => new { id = r.Id }).</param>
    /// <param name="successMessage">Message for successful creation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>201 Created with Location header and response body.</returns>
    protected async Task<ActionResult<Response<T>>> CreateResourceAsync<T>(
        Func<CancellationToken, Task<T>> createAsync,
        string actionName,
        Func<T, object> getRouteValues,
        string successMessage,
        CancellationToken cancellationToken)
    {
        T result = await createAsync(cancellationToken);
        return CreatedAtActionResponse(actionName, getRouteValues(result), result, successMessage);
    }

    /// <summary>
    /// Common pattern for PUT/PATCH: executes update, returns 204 No Content if found, 404 if not found.
    /// </summary>
    /// <typeparam name="T">The response data type.</typeparam>
    /// <param name="id">The entity identifier.</param>
    /// <param name="updateAsync">Delegate to update the resource (returns Result&lt;T&gt;).</param>
    /// <param name="entityName">The name of the entity for 404 message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="onNotFound">Optional callback when entity is not found (e.g., for logging).</param>
    /// <returns>204 No Content if updated, 404 if not found.</returns>
    protected async Task<IActionResult> UpdateOrNotFoundAsync<T>(
        int id,
        Func<int, CancellationToken, Task<Result<T>>> updateAsync,
        string entityName,
        CancellationToken cancellationToken,
        Action<int>? onNotFound = null)
    {
        Result<T> result = await updateAsync(id, cancellationToken);
        if (!result.IsSuccess)
        {
            onNotFound?.Invoke(id);
            return HandleNotFound<T>(entityName, "ID", id);
        }

        return NoContent();
    }

    /// <summary>
    /// Common pattern for DELETE: executes delete, returns 204 No Content if found or already deleted, 404 if not found.
    /// </summary>
    /// <param name="id">The entity identifier.</param>
    /// <param name="deleteAsync">Delegate to delete the resource (returns true if deleted or idempotent, false if not found).</param>
    /// <param name="entityName">The name of the entity for 404 message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <param name="onNotFound">Optional callback when entity is not found (e.g., for logging).</param>
    /// <returns>204 No Content if deleted, 404 if not found.</returns>
    protected async Task<IActionResult> DeleteOrNotFoundAsync(
        int id,
        Func<int, CancellationToken, Task<bool>> deleteAsync,
        string entityName,
        CancellationToken cancellationToken,
        Action<int>? onNotFound = null)
    {
        bool deleted = await deleteAsync(id, cancellationToken);
        if (!deleted)
        {
            onNotFound?.Invoke(id);
            return HandleNotFound<object>(entityName, "ID", id);
        }

        return NoContent();
    }
}
