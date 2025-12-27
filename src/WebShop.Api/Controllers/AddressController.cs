using Asp.Versioning;

using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;

namespace WebShop.Api.Controllers;

/// <summary>
/// Manages address resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="AddressController"/> class.
/// </remarks>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/addresses")]
[Produces("application/json")]
public class AddressController(IAddressService addressService, ILogger<AddressController> logger) : BaseApiController
{
    private readonly IAddressService _addressService = addressService;
    private readonly ILogger<AddressController> _logger = logger;

    /// <summary>
    /// Gets all addresses.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of addresses.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Response<IReadOnlyList<AddressDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<AddressDto>>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<AddressDto> addresses = await _addressService.GetAllAsync(cancellationToken);
        return Ok(Response<IReadOnlyList<AddressDto>>.Success(addresses, "Addresses retrieved successfully"));
    }

    /// <summary>
    /// Gets an address by ID.
    /// </summary>
    /// <param name="id">Address identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Address if found, otherwise 404.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Response<AddressDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<AddressDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<AddressDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        AddressDto? address = await _addressService.GetByIdAsync(id, cancellationToken);
        if (address == null)
        {
            _logger.LogWarning("Address not found. AddressId: {AddressId}", id);
            return HandleNotFound<AddressDto>("Address", "ID", id)
                ?? NotFoundResponse<AddressDto>("Address not found", $"Address with ID {id} not found.");
        }

        return Ok(Response<AddressDto>.Success(address, "Address retrieved successfully"));
    }

    /// <summary>
    /// Gets addresses for a specific customer.
    /// </summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of addresses for the customer.</returns>
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<AddressDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<AddressDto>>>> GetByCustomerId([FromRoute] int customerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<AddressDto> addresses = await _addressService.GetByCustomerIdAsync(customerId, cancellationToken);
        return Ok(Response<IReadOnlyList<AddressDto>>.Success(addresses, "Addresses retrieved successfully"));
    }

    /// <summary>
    /// Creates a new address.
    /// </summary>
    /// <param name="createDto">Address creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created address.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Response<AddressDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<AddressDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<AddressDto>>> Create([FromBody] CreateAddressDto createDto, CancellationToken cancellationToken)
    {
        AddressDto address = await _addressService.CreateAsync(createDto, cancellationToken);
        Response<AddressDto> response = Response<AddressDto>.Success(address, "Address created successfully");
        return CreatedAtAction(nameof(GetById), new { id = address.Id }, response);
    }

    /// <summary>
    /// Updates an existing address (full update).
    /// </summary>
    /// <param name="id">The unique identifier of the address to update (must be greater than 0).</param>
    /// <param name="updateDto">The address update data containing all fields to update.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, 404 Not Found if address doesn't exist, or 400 Bad Request if validation fails.</returns>
    /// <remarks>
    /// This endpoint performs a full update of the address. All fields in <see cref="UpdateAddressDto"/> should be provided.
    /// This is a PUT operation, which is idempotent: sending the same request multiple times produces the same result.
    /// </remarks>
    /// <example>
    /// <code>
    /// PUT /api/v1/addresses/123
    /// {
    ///   "customerId": 456,
    ///   "firstName": "John",
    ///   "lastName": "Doe",
    ///   "address1": "123 Main St",
    ///   "address2": "Apt 4B",
    ///   "city": "New York",
    ///   "zip": "10001"
    /// }
    /// </code>
    /// </example>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<AddressDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<AddressDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateAddressDto updateDto, CancellationToken cancellationToken)
    {
        AddressDto? address = await _addressService.UpdateAsync(id, updateDto, cancellationToken);
        if (address == null)
        {
            _logger.LogWarning("Address not found for update. AddressId: {AddressId}", id);
            return HandleNotFound<AddressDto>("Address", "ID", id)
                ?? NotFoundResponse<AddressDto>("Address not found", $"Address with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Partially updates an address using JSON Patch (RFC 6902).
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="patchDto">The update data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>204 No Content if successful, or 404 Not Found.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<AddressDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<AddressDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Patch(
        [FromRoute] int id,
        [FromBody] UpdateAddressDto patchDto,
        CancellationToken cancellationToken)
    {
        AddressDto? address = await _addressService.UpdateAsync(id, patchDto, cancellationToken);
        if (address == null)
        {
            _logger.LogWarning("Address not found for patch. AddressId: {AddressId}", id);
            return HandleNotFound<AddressDto>("Address", "ID", id)
                ?? NotFoundResponse<AddressDto>("Address not found", $"Address with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes an address (soft delete).
    /// </summary>
    /// <param name="id">The unique identifier of the address to delete (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, or 404 Not Found if address doesn't exist.</returns>
    /// <remarks>
    /// This endpoint performs a soft delete on the address. The address record is marked as inactive (IsActive = false) but not physically removed from the database.
    /// Soft-deleted addresses are excluded from normal queries but can be recovered if needed.
    /// </remarks>
    /// <example>
    /// <code>
    /// DELETE /api/v1/addresses/123
    /// </code>
    /// </example>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        bool deleted = await _addressService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning("Address not found for deletion. AddressId: {AddressId}", id);
            return NotFoundResponse<object>("Address not found", $"Address with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Creates multiple addresses in a batch operation.
    /// </summary>
    /// <param name="createDtos">List of address creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created addresses with generated IDs.</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<AddressDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<AddressDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<AddressDto>>>> CreateBatch([FromBody] IReadOnlyList<CreateAddressDto> createDtos, CancellationToken cancellationToken)
    {
        IReadOnlyList<AddressDto> addresses = await _addressService.CreateBatchAsync(createDtos, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, Response<IReadOnlyList<AddressDto>>.Success(addresses, "Addresses created successfully"));
    }

    /// <summary>
    /// Updates multiple addresses in a batch operation.
    /// </summary>
    /// <param name="updates">List of address updates (ID and update data pairs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The updated addresses.</returns>
    [HttpPut("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<AddressDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<AddressDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<AddressDto>>>> UpdateBatch([FromBody] IReadOnlyList<BatchUpdateRequest<UpdateAddressDto>> updates, CancellationToken cancellationToken)
    {
        IReadOnlyList<(int Id, UpdateAddressDto UpdateDto)> updateList = updates.Select(u => (u.Id, u.Data)).ToList();
        IReadOnlyList<AddressDto> addresses = await _addressService.UpdateBatchAsync(updateList, cancellationToken);
        return Ok(Response<IReadOnlyList<AddressDto>>.Success(addresses, "Addresses updated successfully"));
    }

    /// <summary>
    /// Deletes multiple addresses in a batch operation (soft delete).
    /// </summary>
    /// <param name="ids">List of address IDs to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>List of successfully deleted address IDs.</returns>
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<int>>>> DeleteBatch([FromBody] IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> deletedIds = await _addressService.DeleteBatchAsync(ids, cancellationToken);
        return Ok(Response<IReadOnlyList<int>>.Success(deletedIds, "Addresses deleted successfully"));
    }
}

