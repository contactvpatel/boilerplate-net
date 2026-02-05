using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;

namespace WebShop.Api.Controllers;

/// <summary>
/// Manages customer resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="CustomerController"/> class.
/// </remarks>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/customers")]
[Produces("application/json")]
public class CustomerController(ICustomerService customerService, ILogger<CustomerController> logger) : BaseApiController
{
    private readonly ICustomerService _customerService = customerService;
    private readonly ILogger<CustomerController> _logger = logger;

    /// <summary>
    /// Gets all customers with optional pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize). Use page=0 or omit for non-paginated results.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A paginated list of customers or all customers if pagination is not requested.</returns>
    /// <remarks>
    /// <para><strong>Pagination (Recommended for large datasets)</strong></para>
    /// <para>Use query parameters to paginate results:</para>
    /// <para>- <c>?page=1&amp;pageSize=20</c> - Returns first 20 customers</para>
    /// <para>- <c>?page=2&amp;pageSize=20</c> - Returns customers 21-40</para>
    /// <para></para>
    /// <para><strong>Non-Paginated (Legacy)</strong></para>
    /// <para>Omit pagination parameters or set page=0 to get all customers.</para>
    /// <para><strong>Warning:</strong> Non-paginated requests may cause performance issues with large datasets.</para>
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/customers?page=1&amp;pageSize=20
    /// </code>
    /// </example>
    [HttpGet]
    [ProducesResponseType(typeof(Response<PagedResult<CustomerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<CustomerDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        if (!pagination.IsPaginated)
        {
            IReadOnlyList<CustomerDto> allCustomers = await _customerService.GetAllAsync(cancellationToken);
            return Ok(Response<IReadOnlyList<CustomerDto>>.Success(allCustomers, "Customers retrieved successfully"));
        }

        (IReadOnlyList<CustomerDto> items, int totalCount) = await _customerService.GetPagedAsync(pagination.Page, pagination.PageSize, cancellationToken);
        PagedResult<CustomerDto> pagedResult = new(items, pagination.Page, pagination.PageSize, totalCount);

        return Ok(Response<PagedResult<CustomerDto>>.Success(
            pagedResult,
            $"Retrieved page {pagination.Page} of {pagedResult.TotalPages} ({items.Count} of {totalCount} total customers)"));
    }

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the customer (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The customer if found, otherwise returns 404 Not Found.</returns>
    /// <remarks>
    /// This endpoint retrieves a single customer by their unique identifier. The ID must be a positive integer.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/customers/123
    /// </code>
    /// </example>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<CustomerDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        CustomerDto? customer = await _customerService.GetByIdAsync(id, cancellationToken);
        if (customer == null)
        {
            _logger.LogWarning("Customer not found. CustomerId: {CustomerId}", id);
            return HandleNotFound<CustomerDto>("Customer", "ID", id);
        }

        return Ok(Response<CustomerDto>.Success(customer, "Customer retrieved successfully"));
    }

    /// <summary>
    /// Gets a customer by email address.
    /// </summary>
    /// <param name="email">The email address of the customer (case-insensitive).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The customer if found, otherwise returns 404 Not Found.</returns>
    /// <remarks>
    /// This endpoint retrieves a customer by their email address. Email matching is case-insensitive.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/customers/email/john.doe@example.com
    /// </code>
    /// </example>
    [HttpGet("email/{email}")]
    [ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<CustomerDto>>> GetByEmail([FromRoute] string email, CancellationToken cancellationToken)
    {
        CustomerDto? customer = await _customerService.GetByEmailAsync(email, cancellationToken);
        if (customer == null)
        {
            _logger.LogWarning("Customer not found by email. Email: {Email}", email);
            return HandleNotFound<CustomerDto>("Customer", "Email", email);
        }

        return Ok(Response<CustomerDto>.Success(customer, "Customer retrieved successfully"));
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="createDto">The customer creation data containing name, email, and other required fields.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created customer with generated ID, or 400 Bad Request if validation fails.</returns>
    /// <remarks>
    /// This endpoint creates a new customer in the system. The request body must contain all required fields as defined in <see cref="CreateCustomerDto"/>.
    /// The email address must be unique. Upon successful creation, the response includes a Location header pointing to the new customer resource.
    /// </remarks>
    /// <example>
    /// <code>
    /// POST /api/v1/customers
    /// {
    ///   "name": "John Doe",
    ///   "email": "john.doe@example.com",
    ///   "phone": "+1234567890"
    /// }
    /// </code>
    /// </example>
    [HttpPost]
    [ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<CustomerDto>>> Create([FromBody] CreateCustomerDto createDto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequestResponse<CustomerDto>("Validation failed");
        }

        CustomerDto customer = await _customerService.CreateAsync(createDto, cancellationToken);
        Response<CustomerDto> response = Response<CustomerDto>.Success(customer, "Customer created successfully");
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, response);
    }

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    /// <param name="id">The unique identifier of the customer to update (must be greater than 0).</param>
    /// <param name="updateDto">The customer update data containing fields to modify.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, 404 Not Found if customer doesn't exist, or 400 Bad Request if validation fails.</returns>
    /// <remarks>
    /// This endpoint performs a full update of the customer. All fields in <see cref="UpdateCustomerDto"/> should be provided.
    /// The email address must remain unique if changed. This is a PUT operation, so partial updates are not supported.
    /// </remarks>
    /// <example>
    /// <code>
    /// PUT /api/v1/customers/123
    /// {
    ///   "name": "John Doe Updated",
    ///   "email": "john.doe.updated@example.com",
    ///   "phone": "+1234567890"
    /// }
    /// </code>
    /// </example>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateCustomerDto updateDto, CancellationToken cancellationToken)
    {
        CustomerDto? customer = await _customerService.UpdateAsync(id, updateDto, cancellationToken);
        if (customer == null)
        {
            _logger.LogWarning("Customer not found for update. CustomerId: {CustomerId}", id);
            return HandleNotFound<CustomerDto>("Customer", "ID", id);
        }

        return NoContent();
    }

    /// <summary>
    /// Partially updates a customer using JSON Patch (RFC 6902).
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="patchDto">The update data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>204 No Content if successful, or 404 Not Found.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<CustomerDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Patch(
        [FromRoute] int id,
        [FromBody] UpdateCustomerDto patchDto,
        CancellationToken cancellationToken)
    {
        CustomerDto? customer = await _customerService.UpdateAsync(id, patchDto, cancellationToken);
        if (customer == null)
        {
            _logger.LogWarning("Customer not found for patch. CustomerId: {CustomerId}", id);
            return HandleNotFound<CustomerDto>("Customer", "ID", id);
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a customer (soft delete).
    /// </summary>
    /// <param name="id">The unique identifier of the customer to delete (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, or 404 Not Found if customer doesn't exist.</returns>
    /// <remarks>
    /// This endpoint performs a soft delete on the customer. The customer record is marked as deleted but not physically removed from the database.
    /// Soft-deleted customers are excluded from normal queries but can be recovered if needed.
    /// </remarks>
    /// <example>
    /// <code>
    /// DELETE /api/v1/customers/123
    /// </code>
    /// </example>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        bool deleted = await _customerService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning("Customer not found for deletion. CustomerId: {CustomerId}", id);
            return HandleNotFound<object>("Customer", "ID", id);
        }

        return NoContent();
    }

    /// <summary>
    /// Creates multiple customers in a batch operation.
    /// </summary>
    /// <param name="createDtos">List of customer creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created customers with generated IDs.</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<CustomerDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<CustomerDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<CustomerDto>>>> CreateBatch([FromBody] IReadOnlyList<CreateCustomerDto> createDtos, CancellationToken cancellationToken)
    {
        IReadOnlyList<CustomerDto> customers = await _customerService.CreateBatchAsync(createDtos, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, Response<IReadOnlyList<CustomerDto>>.Success(customers, "Customers created successfully"));
    }

    /// <summary>
    /// Updates multiple customers in a batch operation.
    /// </summary>
    /// <param name="updates">List of customer updates (ID and update data pairs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The updated customers.</returns>
    [HttpPut("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<CustomerDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<CustomerDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<CustomerDto>>>> UpdateBatch([FromBody] IReadOnlyList<BatchUpdateRequest<UpdateCustomerDto>> updates, CancellationToken cancellationToken)
    {
        IReadOnlyList<(int Id, UpdateCustomerDto UpdateDto)> updateList = updates.Select(u => (u.Id, u.Data)).ToList();
        IReadOnlyList<CustomerDto> customers = await _customerService.UpdateBatchAsync(updateList, cancellationToken);
        return Ok(Response<IReadOnlyList<CustomerDto>>.Success(customers, "Customers updated successfully"));
    }

    /// <summary>
    /// Deletes multiple customers in a batch operation (soft delete).
    /// </summary>
    /// <param name="ids">List of customer IDs to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>List of successfully deleted customer IDs.</returns>
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<int>>>> DeleteBatch([FromBody] IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> deletedIds = await _customerService.DeleteBatchAsync(ids, cancellationToken);
        return Ok(Response<IReadOnlyList<int>>.Success(deletedIds, "Customers deleted successfully"));
    }
}

