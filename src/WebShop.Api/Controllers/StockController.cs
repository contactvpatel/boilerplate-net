using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;

namespace WebShop.Api.Controllers;

/// <summary>
/// Manages stock resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="StockController"/> class.
/// </remarks>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/stocks")]
[Produces("application/json")]
public class StockController(IStockService stockService, ILogger<StockController> logger) : BaseApiController
{
    /// <summary>
    /// Gets all stock entries.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of stock entries.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Response<IReadOnlyList<StockDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<StockDto>>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<StockDto> stock = await stockService.GetAllAsync(cancellationToken);
        return OkResponse(stock, "Stock entries retrieved successfully");
    }

    /// <summary>
    /// Gets a stock entry by ID.
    /// </summary>
    /// <param name="id">Stock identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stock entry if found, otherwise 404.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Response<StockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<StockDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<StockDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        return await GetByIdOrNotFoundAsync(
            id,
            stockService.GetByIdAsync,
            "Stock entry",
            "Stock entry retrieved successfully",
            cancellationToken,
            id => logger.LogWarning("Stock entry not found. StockId: {StockId}", id));
    }

    /// <summary>
    /// Gets stock information for a specific article.
    /// </summary>
    /// <param name="articleId">Article identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Stock entry if found, otherwise 404.</returns>
    [HttpGet("article/{articleId}")]
    [ProducesResponseType(typeof(Response<StockDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<StockDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<StockDto>>> GetByArticleId([FromRoute] int articleId, CancellationToken cancellationToken)
    {
        return await GetByPropertyOrNotFoundAsync(
            ct => stockService.GetByArticleIdAsync(articleId, ct),
            "Stock",
            "ArticleID",
            articleId,
            "Stock entry retrieved successfully",
            cancellationToken,
            () => logger.LogWarning("Stock entry not found for article. ArticleId: {ArticleId}", articleId));
    }

    /// <summary>
    /// Gets all stock entries with low inventory.
    /// </summary>
    /// <param name="threshold">Minimum stock threshold.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of stock entries below the threshold.</returns>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<StockDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<StockDto>>>> GetLowStock(
        [FromQuery] int threshold = 10,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<StockDto> stock = await stockService.GetLowStockAsync(threshold, cancellationToken);
        return OkResponse(stock, "Low stock entries retrieved successfully");
    }

    /// <summary>
    /// Creates a new stock entry.
    /// </summary>
    /// <param name="createDto">The stock creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created stock entry with generated ID, or 400 Bad Request if validation fails.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Response<StockDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<StockDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<StockDto>>> Create([FromBody] CreateStockDto createDto, CancellationToken cancellationToken)
    {
        return await CreateResourceAsync(
            ct => stockService.CreateAsync(createDto, ct),
            nameof(GetById),
            r => new { id = r.Id },
            "Stock entry created successfully",
            cancellationToken);
    }

    /// <summary>
    /// Updates an existing stock entry (full update).
    /// </summary>
    /// <param name="id">The unique identifier of the stock entry to update (must be greater than 0).</param>
    /// <param name="updateDto">The stock update data containing all fields to update.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, 404 Not Found if stock entry doesn't exist, or 400 Bad Request if validation fails.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<StockDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<StockDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateStockDto updateDto, CancellationToken cancellationToken)
    {
        return await UpdateOrNotFoundAsync(
            id,
            (identifier, ct) => stockService.UpdateAsync(identifier, updateDto, ct),
            "Stock entry",
            cancellationToken,
            identifier => logger.LogWarning("Stock entry not found for update. StockId: {StockId}", identifier));
    }

    /// <summary>
    /// Partially updates stock using JSON Patch (RFC 6902).
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="patchDto">The update data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>204 No Content if successful, or 404 Not Found.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<StockDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<StockDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Patch(
        [FromRoute] int id,
        [FromBody] UpdateStockDto patchDto,
        CancellationToken cancellationToken)
    {
        return await UpdateOrNotFoundAsync(
            id,
            (identifier, ct) => stockService.PatchAsync(identifier, patchDto, ct),
            "Stock entry",
            cancellationToken,
            identifier => logger.LogWarning("Stock not found for patch. StockId: {StockId}", identifier));
    }

    /// <summary>
    /// Deletes a stock entry (soft delete).
    /// </summary>
    /// <param name="id">The unique identifier of the stock entry to delete (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, or 404 Not Found if stock entry doesn't exist.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        return await DeleteOrNotFoundAsync(
            id,
            stockService.DeleteAsync,
            "Stock entry",
            cancellationToken,
            identifier => logger.LogWarning("Stock entry not found for deletion. StockId: {StockId}", identifier));
    }

    /// <summary>
    /// Creates multiple stock entries in a batch operation.
    /// </summary>
    /// <param name="createDtos">List of stock creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created stock entries with generated IDs.</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<StockDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<StockDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<StockDto>>>> CreateBatch([FromBody] IReadOnlyList<CreateStockDto> createDtos, CancellationToken cancellationToken)
    {
        IReadOnlyList<StockDto> stocks = await stockService.CreateBatchAsync(createDtos, cancellationToken);
        return CreatedResponse(stocks, "Stock entries created successfully");
    }

    /// <summary>
    /// Updates multiple stock entries in a batch operation.
    /// </summary>
    /// <param name="updates">List of stock updates (ID and update data pairs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The updated stock entries.</returns>
    [HttpPut("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<StockDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<StockDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<StockDto>>>> UpdateBatch([FromBody] IReadOnlyList<BatchUpdateRequest<UpdateStockDto>> updates, CancellationToken cancellationToken)
    {
        IReadOnlyList<(int Id, UpdateStockDto UpdateDto)> updateList = updates.Select(u => (u.Id, u.Data)).ToList();
        IReadOnlyList<StockDto> stocks = await stockService.UpdateBatchAsync(updateList, cancellationToken);
        return OkResponse(stocks, "Stock entries updated successfully");
    }

    /// <summary>
    /// Deletes multiple stock entries in a batch operation (soft delete).
    /// </summary>
    /// <param name="ids">List of stock entry IDs to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>List of successfully deleted stock entry IDs.</returns>
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<int>>>> DeleteBatch([FromBody] IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> deletedIds = await stockService.DeleteBatchAsync(ids, cancellationToken);
        return OkResponse(deletedIds, "Stock entries deleted successfully");
    }
}
