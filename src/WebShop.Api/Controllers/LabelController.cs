using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;

namespace WebShop.Api.Controllers;

/// <summary>
/// Manages label/brand resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="LabelController"/> class.
/// </remarks>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/labels")]
[Produces("application/json")]
public class LabelController(ILabelService labelService, ILogger<LabelController> logger) : BaseApiController
{
    /// <summary>
    /// Gets all labels.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of labels.</returns>
    /// <remarks>
    /// This endpoint is cached for 5 minutes as label data is reference data that changes infrequently.
    /// </remarks>
    [HttpGet]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept,Accept-Encoding")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<LabelDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<LabelDto>>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<LabelDto> labels = await labelService.GetAllAsync(cancellationToken);
        return OkResponse(labels, "Labels retrieved successfully");
    }

    /// <summary>
    /// Gets a label by ID.
    /// </summary>
    /// <param name="id">Label identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Label if found, otherwise 404.</returns>
    /// <remarks>
    /// This endpoint is cached for 5 minutes as label data is reference data that changes infrequently.
    /// </remarks>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept,Accept-Encoding")]
    [ProducesResponseType(typeof(Response<LabelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<LabelDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<LabelDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        return await GetByIdOrNotFoundAsync(
            id,
            labelService.GetByIdAsync,
            "Label",
            "Label retrieved successfully",
            cancellationToken,
            id => logger.LogWarning("Label not found. LabelId: {LabelId}", id));
    }

    /// <summary>
    /// Gets a label by slug name.
    /// </summary>
    /// <param name="slugName">Slug name of the label.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Label if found, otherwise 404.</returns>
    /// <remarks>
    /// This endpoint is cached for 5 minutes as label data is reference data that changes infrequently.
    /// </remarks>
    [HttpGet("slug/{slugName}")]
    [ResponseCache(Duration = 300, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept,Accept-Encoding")]
    [ProducesResponseType(typeof(Response<LabelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<LabelDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<LabelDto>>> GetBySlugName([FromRoute] string slugName, CancellationToken cancellationToken)
    {
        return await GetByPropertyOrNotFoundAsync(
            ct => labelService.GetBySlugNameAsync(slugName, ct),
            "Label",
            "SlugName",
            slugName,
            "Label retrieved successfully",
            cancellationToken,
            () => logger.LogWarning("Label not found by slug name. SlugName: {SlugName}", slugName));
    }

    /// <summary>
    /// Creates a new label/brand.
    /// </summary>
    /// <param name="createDto">The label creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created label with generated ID, or 400 Bad Request if validation fails.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Response<LabelDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<LabelDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<LabelDto>>> Create([FromBody] CreateLabelDto createDto, CancellationToken cancellationToken)
    {
        return await CreateResourceAsync(
            ct => labelService.CreateAsync(createDto, ct),
            nameof(GetById),
            r => new { id = r.Id },
            "Label created successfully",
            cancellationToken);
    }

    /// <summary>
    /// Updates an existing label (full update).
    /// </summary>
    /// <param name="id">The unique identifier of the label to update (must be greater than 0).</param>
    /// <param name="updateDto">The label update data containing all fields to update.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, 404 Not Found if label doesn't exist, or 400 Bad Request if validation fails.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<LabelDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<LabelDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateLabelDto updateDto, CancellationToken cancellationToken)
    {
        return await UpdateOrNotFoundAsync(
            id,
            (identifier, ct) => labelService.UpdateAsync(identifier, updateDto, ct),
            "Label",
            cancellationToken,
            identifier => logger.LogWarning("Label not found for update. LabelId: {LabelId}", identifier));
    }

    /// <summary>
    /// Partially updates a label using JSON Patch (RFC 6902).
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="patchDto">The update data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>204 No Content if successful, or 404 Not Found.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<LabelDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<LabelDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Patch(
        [FromRoute] int id,
        [FromBody] UpdateLabelDto patchDto,
        CancellationToken cancellationToken)
    {
        return await UpdateOrNotFoundAsync(
            id,
            (identifier, ct) => labelService.PatchAsync(identifier, patchDto, ct),
            "Label",
            cancellationToken,
            identifier => logger.LogWarning("Label not found for patch. LabelId: {LabelId}", identifier));
    }

    /// <summary>
    /// Deletes a label (soft delete).
    /// </summary>
    /// <param name="id">The unique identifier of the label to delete (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, or 404 Not Found if label doesn't exist.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        return await DeleteOrNotFoundAsync(
            id,
            labelService.DeleteAsync,
            "Label",
            cancellationToken,
            identifier => logger.LogWarning("Label not found for deletion. LabelId: {LabelId}", identifier));
    }

    /// <summary>
    /// Creates multiple labels in a batch operation.
    /// </summary>
    /// <param name="createDtos">List of label creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created labels with generated IDs.</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<LabelDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<LabelDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<LabelDto>>>> CreateBatch([FromBody] IReadOnlyList<CreateLabelDto> createDtos, CancellationToken cancellationToken)
    {
        IReadOnlyList<LabelDto> labels = await labelService.CreateBatchAsync(createDtos, cancellationToken);
        return CreatedResponse(labels, "Labels created successfully");
    }

    /// <summary>
    /// Updates multiple labels in a batch operation.
    /// </summary>
    /// <param name="updates">List of label updates (ID and update data pairs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The updated labels.</returns>
    [HttpPut("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<LabelDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<LabelDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<LabelDto>>>> UpdateBatch([FromBody] IReadOnlyList<BatchUpdateRequest<UpdateLabelDto>> updates, CancellationToken cancellationToken)
    {
        IReadOnlyList<(int Id, UpdateLabelDto UpdateDto)> updateList = updates.Select(u => (u.Id, u.Data)).ToList();
        IReadOnlyList<LabelDto> labels = await labelService.UpdateBatchAsync(updateList, cancellationToken);
        return OkResponse(labels, "Labels updated successfully");
    }

    /// <summary>
    /// Deletes multiple labels in a batch operation (soft delete).
    /// </summary>
    /// <param name="ids">List of label IDs to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>List of successfully deleted label IDs.</returns>
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<int>>>> DeleteBatch([FromBody] IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> deletedIds = await labelService.DeleteBatchAsync(ids, cancellationToken);
        return OkResponse(deletedIds, "Labels deleted successfully");
    }
}
