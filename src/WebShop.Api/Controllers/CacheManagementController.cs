using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Api.Controllers;

/// <summary>
/// Manages cache operations for administrative purposes.
/// Provides endpoints to clear cache entries by key, keys, tag, or tags.
/// </summary>
/// <remarks>
/// This controller should be secured and only accessible to administrators.
/// Cache clearing affects both in-memory and distributed cache (if configured).
/// </remarks>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/cache-management")]
[Produces("application/json")]
public class CacheManagementController(
    ICacheService cacheService,
    ILogger<CacheManagementController> logger) : BaseApiController
{
    /// <summary>
    /// Clears multiple cache entries by their keys.
    /// </summary>
    /// <param name="request">Request containing the list of cache keys to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the cache operation.</returns>
    [HttpDelete("keys")]
    [ProducesResponseType(typeof(Response<CacheOperationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<CacheOperationResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<CacheOperationResultDto>>> ClearByKeys(
        [FromBody] ClearCacheByKeysRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            int keyCount = request.Keys.Count;
            await cacheService.RemoveAsync(request.Keys, cancellationToken);

            CacheOperationResultDto result = new()
            {
                IsSuccess = true,
                EntriesAffected = keyCount,
                Message = $"Successfully cleared {keyCount} cache entry/entries."
            };

            return OkResponse(result, "Cache entries cleared successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing cache entries by keys");
            return InternalServerErrorResponse<CacheOperationResultDto>("Failed to clear cache entries", $"An error occurred while clearing the cache entries: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all cache entries associated with a specific tag.
    /// </summary>
    /// <param name="request">Request containing the tag to remove entries for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the cache operation.</returns>
    [HttpDelete("tag")]
    [ProducesResponseType(typeof(Response<CacheOperationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<CacheOperationResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<CacheOperationResultDto>>> ClearByTag(
        [FromBody] ClearCacheByTagRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            await cacheService.RemoveByTagAsync(request.Tag, cancellationToken);

            CacheOperationResultDto result = new()
            {
                IsSuccess = true,
                EntriesAffected = -1, // Unknown count for tag-based removal
                Message = $"All cache entries associated with tag '{request.Tag}' have been cleared successfully."
            };

            return OkResponse(result, "Cache entries cleared by tag successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing cache entries by tag: {Tag}", request.Tag);
            return InternalServerErrorResponse<CacheOperationResultDto>("Failed to clear cache entries by tag", $"An error occurred while clearing cache entries by tag: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears all cache entries associated with multiple tags.
    /// </summary>
    /// <param name="request">Request containing the list of tags to remove entries for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the cache operation.</returns>
    [HttpDelete("tags")]
    [ProducesResponseType(typeof(Response<CacheOperationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<CacheOperationResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<CacheOperationResultDto>>> ClearByTags(
        [FromBody] ClearCacheByTagsRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            int tagCount = request.Tags.Count;
            await cacheService.RemoveByTagAsync(request.Tags, cancellationToken);

            CacheOperationResultDto result = new()
            {
                IsSuccess = true,
                EntriesAffected = -1, // Unknown count for tag-based removal
                Message = $"All cache entries associated with {tagCount} tag(s) have been cleared successfully."
            };

            return OkResponse(result, "Cache entries cleared by tags successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing cache entries by tags");
            return InternalServerErrorResponse<CacheOperationResultDto>("Failed to clear cache entries by tags", $"An error occurred while clearing cache entries by tags: {ex.Message}");
        }
    }

    /// <summary>
    /// Clears a cache entry by its key.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result of the cache operation.</returns>
    [HttpDelete("key/{key}")]
    [ProducesResponseType(typeof(Response<CacheOperationResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<CacheOperationResultDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<CacheOperationResultDto>>> ClearByKey(
        [FromRoute] string key,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return BadRequestResponse<CacheOperationResultDto>("Validation failed", "Cache key cannot be null or empty.");
        }

        try
        {
            await cacheService.RemoveAsync(key, cancellationToken);

            CacheOperationResultDto result = new()
            {
                IsSuccess = true,
                EntriesAffected = 1,
                Message = $"Cache entry with key '{key}' has been cleared successfully."
            };

            return OkResponse(result, "Cache entry cleared successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing cache entry by key: {Key}", key);
            return InternalServerErrorResponse<CacheOperationResultDto>("Failed to clear cache entry", $"An error occurred while clearing the cache entry: {ex.Message}");
        }
    }
}

