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
        IReadOnlyList<ColorDto> colors = await colorService.GetAllAsync(cancellationToken);
        return OkResponse(colors, "Colors retrieved successfully");
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
        return await GetByIdOrNotFoundAsync(
            id,
            colorService.GetByIdAsync,
            "Color",
            "Color retrieved successfully",
            cancellationToken,
            id => logger.LogWarning("Color not found. ColorId: {ColorId}", id));
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
        return await GetByPropertyOrNotFoundAsync(
            ct => colorService.GetByNameAsync(name, ct),
            "Color",
            "Name",
            name,
            "Color retrieved successfully",
            cancellationToken,
            () => logger.LogWarning("Color not found by name. Name: {Name}", name));
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
        return await CreateResourceAsync(
            ct => colorService.CreateAsync(createDto, ct),
            nameof(GetById),
            r => new { id = r.Id },
            "Color created successfully",
            cancellationToken);
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
        return await UpdateOrNotFoundAsync(
            id,
            (identifier, ct) => colorService.UpdateAsync(identifier, updateDto, ct),
            "Color",
            cancellationToken,
            identifier => logger.LogWarning("Color not found for update. ColorId: {ColorId}", identifier));
    }

    /// <summary>
    /// Partially updates a color (merge semantics). Only provided fields are updated; null/omitted fields are unchanged.
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
        return await UpdateOrNotFoundAsync(
            id,
            (identifier, ct) => colorService.PatchAsync(identifier, patchDto, ct),
            "Color",
            cancellationToken,
            identifier => logger.LogWarning("Color not found for patch. ColorId: {ColorId}", identifier));
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
        return await DeleteOrNotFoundAsync(
            id,
            colorService.DeleteAsync,
            "Color",
            cancellationToken,
            identifier => logger.LogWarning("Color not found for deletion. ColorId: {ColorId}", identifier));
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
        IReadOnlyList<ColorDto> colors = await colorService.CreateBatchAsync(createDtos, cancellationToken);
        return CreatedResponse(colors, "Colors created successfully");
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
        IReadOnlyList<ColorDto> colors = await colorService.UpdateBatchAsync(updateList, cancellationToken);
        return OkResponse(colors, "Colors updated successfully");
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
        IReadOnlyList<int> deletedIds = await colorService.DeleteBatchAsync(ids, cancellationToken);
        return OkResponse(deletedIds, "Colors deleted successfully");
    }
}
