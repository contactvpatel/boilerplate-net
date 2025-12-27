using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;

namespace WebShop.Api.Controllers;

/// <summary>
/// Manages size resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SizeController"/> class.
/// </remarks>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/sizes")]
[Produces("application/json")]
public class SizeController(ISizeService sizeService, ILogger<SizeController> logger) : BaseApiController
{
    private readonly ISizeService _sizeService = sizeService;
    private readonly ILogger<SizeController> _logger = logger;

    /// <summary>
    /// Gets all sizes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sizes.</returns>
    /// <remarks>
    /// This endpoint is cached for 5 minutes as size data is reference data that changes infrequently.
    /// </remarks>
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept,Accept-Encoding")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<SizeDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<SizeDto>>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<SizeDto> sizes = await _sizeService.GetAllAsync(cancellationToken);
        return Ok(Response<IReadOnlyList<SizeDto>>.Success(sizes, "Sizes retrieved successfully"));
    }

    /// <summary>
    /// Gets a size by ID.
    /// </summary>
    /// <param name="id">Size identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Size if found, otherwise 404.</returns>
    /// <remarks>
    /// This endpoint is cached for 5 minutes as size data is reference data that changes infrequently.
    /// </remarks>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept,Accept-Encoding")]
    [ProducesResponseType(typeof(Response<SizeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<SizeDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<SizeDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        SizeDto? size = await _sizeService.GetByIdAsync(id, cancellationToken);
        if (size == null)
        {
            _logger.LogWarning("Size not found. SizeId: {SizeId}", id);
            return HandleNotFound<SizeDto>("Size", "ID", id)
                ?? NotFoundResponse<SizeDto>("Size not found", $"Size with ID {id} not found.");
        }

        return Ok(Response<SizeDto>.Success(size, "Size retrieved successfully"));
    }

    /// <summary>
    /// Gets sizes by gender and category.
    /// </summary>
    /// <param name="gender">Gender classification.</param>
    /// <param name="category">Product category.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of sizes matching the criteria.</returns>
    /// <remarks>
    /// This endpoint is cached for 5 minutes as size data is reference data that changes infrequently.
    /// Cache varies by gender and category parameters.
    /// </remarks>
    [HttpGet("gender/{gender}/category/{category}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept,Accept-Encoding", VaryByQueryKeys = new[] { "gender", "category" })]
    [ProducesResponseType(typeof(Response<IReadOnlyList<SizeDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<SizeDto>>>> GetByGenderAndCategory(
        [FromRoute] string gender,
        [FromRoute] string category,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SizeDto> sizes = await _sizeService.GetByGenderAndCategoryAsync(gender, category, cancellationToken);
        return Ok(Response<IReadOnlyList<SizeDto>>.Success(sizes, "Sizes retrieved successfully"));
    }

    /// <summary>
    /// Creates a new size.
    /// </summary>
    /// <param name="createDto">The size creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created size with generated ID, or 400 Bad Request if validation fails.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Response<SizeDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<SizeDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<SizeDto>>> Create([FromBody] CreateSizeDto createDto, CancellationToken cancellationToken)
    {
        SizeDto size = await _sizeService.CreateAsync(createDto, cancellationToken);
        Response<SizeDto> response = Response<SizeDto>.Success(size, "Size created successfully");
        return CreatedAtAction(nameof(GetById), new { id = size.Id }, response);
    }

    /// <summary>
    /// Updates an existing size (full update).
    /// </summary>
    /// <param name="id">The unique identifier of the size to update (must be greater than 0).</param>
    /// <param name="updateDto">The size update data containing all fields to update.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, 404 Not Found if size doesn't exist, or 400 Bad Request if validation fails.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<SizeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<SizeDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateSizeDto updateDto, CancellationToken cancellationToken)
    {
        SizeDto? size = await _sizeService.UpdateAsync(id, updateDto, cancellationToken);
        if (size == null)
        {
            _logger.LogWarning("Size not found for update. SizeId: {SizeId}", id);
            return HandleNotFound<SizeDto>("Size", "ID", id)
                ?? NotFoundResponse<SizeDto>("Size not found", $"Size with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Partially updates a size using JSON Patch (RFC 6902).
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="patchDto">The update data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>204 No Content if successful, or 404 Not Found.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<SizeDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<SizeDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Patch(
        [FromRoute] int id,
        [FromBody] UpdateSizeDto patchDto,
        CancellationToken cancellationToken)
    {
        SizeDto? size = await _sizeService.UpdateAsync(id, patchDto, cancellationToken);
        if (size == null)
        {
            _logger.LogWarning("Size not found for patch. SizeId: {SizeId}", id);
            return HandleNotFound<SizeDto>("Size", "ID", id)
                ?? NotFoundResponse<SizeDto>("Size not found", $"Size with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a size (soft delete).
    /// </summary>
    /// <param name="id">The unique identifier of the size to delete (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, or 404 Not Found if size doesn't exist.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        bool deleted = await _sizeService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning("Size not found for deletion. SizeId: {SizeId}", id);
            return NotFoundResponse<object>("Size not found", $"Size with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Creates multiple sizes in a batch operation.
    /// </summary>
    /// <param name="createDtos">List of size creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created sizes with generated IDs.</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<SizeDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<SizeDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<SizeDto>>>> CreateBatch([FromBody] IReadOnlyList<CreateSizeDto> createDtos, CancellationToken cancellationToken)
    {
        IReadOnlyList<SizeDto> sizes = await _sizeService.CreateBatchAsync(createDtos, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, Response<IReadOnlyList<SizeDto>>.Success(sizes, "Sizes created successfully"));
    }

    /// <summary>
    /// Updates multiple sizes in a batch operation.
    /// </summary>
    /// <param name="updates">List of size updates (ID and update data pairs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The updated sizes.</returns>
    [HttpPut("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<SizeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<SizeDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<SizeDto>>>> UpdateBatch([FromBody] IReadOnlyList<BatchUpdateRequest<UpdateSizeDto>> updates, CancellationToken cancellationToken)
    {
        IReadOnlyList<(int Id, UpdateSizeDto UpdateDto)> updateList = updates.Select(u => (u.Id, u.Data)).ToList();
        IReadOnlyList<SizeDto> sizes = await _sizeService.UpdateBatchAsync(updateList, cancellationToken);
        return Ok(Response<IReadOnlyList<SizeDto>>.Success(sizes, "Sizes updated successfully"));
    }

    /// <summary>
    /// Deletes multiple sizes in a batch operation (soft delete).
    /// </summary>
    /// <param name="ids">List of size IDs to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>List of successfully deleted size IDs.</returns>
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<int>>>> DeleteBatch([FromBody] IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> deletedIds = await _sizeService.DeleteBatchAsync(ids, cancellationToken);
        return Ok(Response<IReadOnlyList<int>>.Success(deletedIds, "Sizes deleted successfully"));
    }
}
