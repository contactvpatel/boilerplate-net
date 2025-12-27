using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;

namespace WebShop.Api.Controllers;

/// <summary>
/// Manages color resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ColorController"/> class.
/// </remarks>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/colors")]
[Produces("application/json")]
public class ColorController(IColorService colorService, ILogger<ColorController> logger) : BaseApiController
{
    private readonly IColorService _colorService = colorService;
    private readonly ILogger<ColorController> _logger = logger;

    /// <summary>
    /// Gets all colors.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of colors.</returns>
    /// <remarks>
    /// This endpoint is cached for 5 minutes as color data is reference data that changes infrequently.
    /// </remarks>
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept,Accept-Encoding")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ColorDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<ColorDto>>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<ColorDto> colors = await _colorService.GetAllAsync(cancellationToken);
        return Ok(Response<IReadOnlyList<ColorDto>>.Success(colors, "Colors retrieved successfully"));
    }

    /// <summary>
    /// Gets a color by ID.
    /// </summary>
    /// <param name="id">Color identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Color if found, otherwise 404.</returns>
    /// <remarks>
    /// This endpoint is cached for 5 minutes as color data is reference data that changes infrequently.
    /// </remarks>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept,Accept-Encoding")]
    [ProducesResponseType(typeof(Response<ColorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<ColorDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<ColorDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        ColorDto? color = await _colorService.GetByIdAsync(id, cancellationToken);
        if (color == null)
        {
            _logger.LogWarning("Color not found. ColorId: {ColorId}", id);
            return HandleNotFound<ColorDto>("Color", "ID", id)
                ?? NotFoundResponse<ColorDto>("Color not found", $"Color with ID {id} not found.");
        }

        return Ok(Response<ColorDto>.Success(color, "Color retrieved successfully"));
    }

    /// <summary>
    /// Gets a color by name.
    /// </summary>
    /// <param name="name">Color name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Color if found, otherwise 404.</returns>
    /// <remarks>
    /// This endpoint is cached for 5 minutes as color data is reference data that changes infrequently.
    /// </remarks>
    [HttpGet("name/{name}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept,Accept-Encoding")]
    [ProducesResponseType(typeof(Response<ColorDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<ColorDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<ColorDto>>> GetByName([FromRoute] string name, CancellationToken cancellationToken)
    {
        ColorDto? color = await _colorService.GetByNameAsync(name, cancellationToken);
        if (color == null)
        {
            _logger.LogWarning("Color not found by name. Name: {Name}", name);
            return NotFoundResponse<ColorDto>("Color not found", $"Color with name {name} not found.");
        }

        return Ok(Response<ColorDto>.Success(color, "Color retrieved successfully"));
    }

    /// <summary>
    /// Creates a new color.
    /// </summary>
    /// <param name="createDto">The color creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created color with generated ID, or 400 Bad Request if validation fails.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Response<ColorDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<ColorDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<ColorDto>>> Create([FromBody] CreateColorDto createDto, CancellationToken cancellationToken)
    {
        ColorDto color = await _colorService.CreateAsync(createDto, cancellationToken);
        Response<ColorDto> response = Response<ColorDto>.Success(color, "Color created successfully");
        return CreatedAtAction(nameof(GetById), new { id = color.Id }, response);
    }

    /// <summary>
    /// Updates an existing color (full update).
    /// </summary>
    /// <param name="id">The unique identifier of the color to update (must be greater than 0).</param>
    /// <param name="updateDto">The color update data containing all fields to update.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, 404 Not Found if color doesn't exist, or 400 Bad Request if validation fails.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<ColorDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<ColorDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateColorDto updateDto, CancellationToken cancellationToken)
    {
        ColorDto? color = await _colorService.UpdateAsync(id, updateDto, cancellationToken);
        if (color == null)
        {
            _logger.LogWarning("Color not found for update. ColorId: {ColorId}", id);
            return HandleNotFound<ColorDto>("Color", "ID", id)
                ?? NotFoundResponse<ColorDto>("Color not found", $"Color with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Partially updates a color using JSON Patch (RFC 6902).
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="patchDto">The update data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>204 No Content if successful, or 404 Not Found.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<ColorDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<ColorDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Patch(
        [FromRoute] int id,
        [FromBody] UpdateColorDto patchDto,
        CancellationToken cancellationToken)
    {
        ColorDto? color = await _colorService.UpdateAsync(id, patchDto, cancellationToken);
        if (color == null)
        {
            _logger.LogWarning("Color not found for patch. ColorId: {ColorId}", id);
            return HandleNotFound<ColorDto>("Color", "ID", id)
                ?? NotFoundResponse<ColorDto>("Color not found", $"Color with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a color (soft delete).
    /// </summary>
    /// <param name="id">The unique identifier of the color to delete (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, or 404 Not Found if color doesn't exist.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        bool deleted = await _colorService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning("Color not found for deletion. ColorId: {ColorId}", id);
            return NotFoundResponse<object>("Color not found", $"Color with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Creates multiple colors in a batch operation.
    /// </summary>
    /// <param name="createDtos">List of color creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created colors with generated IDs.</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ColorDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ColorDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<ColorDto>>>> CreateBatch([FromBody] IReadOnlyList<CreateColorDto> createDtos, CancellationToken cancellationToken)
    {
        IReadOnlyList<ColorDto> colors = await _colorService.CreateBatchAsync(createDtos, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, Response<IReadOnlyList<ColorDto>>.Success(colors, "Colors created successfully"));
    }

    /// <summary>
    /// Updates multiple colors in a batch operation.
    /// </summary>
    /// <param name="updates">List of color updates (ID and update data pairs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The updated colors.</returns>
    [HttpPut("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ColorDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ColorDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<ColorDto>>>> UpdateBatch([FromBody] IReadOnlyList<BatchUpdateRequest<UpdateColorDto>> updates, CancellationToken cancellationToken)
    {
        IReadOnlyList<(int Id, UpdateColorDto UpdateDto)> updateList = updates.Select(u => (u.Id, u.Data)).ToList();
        IReadOnlyList<ColorDto> colors = await _colorService.UpdateBatchAsync(updateList, cancellationToken);
        return Ok(Response<IReadOnlyList<ColorDto>>.Success(colors, "Colors updated successfully"));
    }

    /// <summary>
    /// Deletes multiple colors in a batch operation (soft delete).
    /// </summary>
    /// <param name="ids">List of color IDs to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>List of successfully deleted color IDs.</returns>
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<int>>>> DeleteBatch([FromBody] IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> deletedIds = await _colorService.DeleteBatchAsync(ids, cancellationToken);
        return Ok(Response<IReadOnlyList<int>>.Success(deletedIds, "Colors deleted successfully"));
    }
}
