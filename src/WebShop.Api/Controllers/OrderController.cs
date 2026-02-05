using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;

namespace WebShop.Api.Controllers;

/// <summary>
/// Manages order resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="OrderController"/> class.
/// </remarks>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/orders")]
[Produces("application/json")]
public class OrderController(IOrderService orderService, ILogger<OrderController> logger) : BaseApiController
{
    private readonly IOrderService _orderService = orderService;
    private readonly ILogger<OrderController> _logger = logger;

    /// <summary>
    /// Gets all orders with optional pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize). Use page=0 or omit for non-paginated results.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A paginated list of orders or all orders if pagination is not requested.</returns>
    /// <remarks>
    /// <para><strong>Pagination (Recommended)</strong>: Use <c>?page=1&amp;pageSize=20</c></para>
    /// <para><strong>Non-Paginated (Legacy)</strong>: Omit pagination parameters.</para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(Response<PagedResult<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        if (!pagination.IsPaginated)
        {
            IReadOnlyList<OrderDto> allOrders = await _orderService.GetAllAsync(cancellationToken);
            return Ok(Response<IReadOnlyList<OrderDto>>.Success(allOrders, "Orders retrieved successfully"));
        }

        (IReadOnlyList<OrderDto> items, int totalCount) = await _orderService.GetPagedAsync(pagination.Page, pagination.PageSize, cancellationToken);
        PagedResult<OrderDto> pagedResult = new(items, pagination.Page, pagination.PageSize, totalCount);

        return Ok(Response<PagedResult<OrderDto>>.Success(
            pagedResult,
            $"Retrieved page {pagination.Page} of {pagedResult.TotalPages} ({items.Count} of {totalCount} total orders)"));
    }

    /// <summary>
    /// Gets an order by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the order (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The order if found, otherwise returns 404 Not Found.</returns>
    /// <remarks>
    /// This endpoint retrieves a single order by its unique identifier, including all order items and customer information.
    /// The ID must be a positive integer.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/orders/789
    /// </code>
    /// </example>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Response<OrderDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<OrderDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<OrderDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        OrderDto? order = await _orderService.GetByIdAsync(id, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order not found. OrderId: {OrderId}", id);
            return HandleNotFound<OrderDto>("Order", "ID", id);
        }

        return Ok(Response<OrderDto>.Success(order, "Order retrieved successfully"));
    }

    /// <summary>
    /// Gets orders for a specific customer.
    /// </summary>
    /// <param name="customerId">The unique identifier of the customer (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A list of all orders placed by the specified customer.</returns>
    /// <remarks>
    /// This endpoint retrieves all orders associated with a specific customer. Returns an empty list if the customer has no orders.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/orders/customer/123
    /// </code>
    /// </example>
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<OrderDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<OrderDto>>>> GetByCustomerId([FromRoute] int customerId, CancellationToken cancellationToken)
    {
        IReadOnlyList<OrderDto> orders = await _orderService.GetByCustomerIdAsync(customerId, cancellationToken);
        return Ok(Response<IReadOnlyList<OrderDto>>.Success(orders, "Orders retrieved successfully"));
    }

    /// <summary>
    /// Gets orders within a date range.
    /// </summary>
    /// <param name="startDate">The start date of the range (inclusive). Format: ISO 8601 (e.g., 2024-01-01T00:00:00Z).</param>
    /// <param name="endDate">The end date of the range (inclusive). Format: ISO 8601 (e.g., 2024-12-31T23:59:59Z).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A list of orders created within the specified date range, or 400 if end date is before start date.</returns>
    /// <remarks>
    /// This endpoint retrieves all orders that were created between the start date and end date (inclusive).
    /// Both dates are required query parameters. The end date must be greater than or equal to the start date; otherwise 400 Bad Request is returned.
    /// Returns an empty list if no orders are found in the specified range.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/orders/date-range?startDate=2024-01-01T00:00:00Z&amp;endDate=2024-12-31T23:59:59Z
    /// </code>
    /// </example>
    [HttpGet("date-range")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<OrderDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<OrderDto>>>> GetByDateRange(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        CancellationToken cancellationToken)
    {
        if (endDate < startDate)
        {
            return BadRequestResponse<IReadOnlyList<OrderDto>>(
                "Invalid date range",
                "End date must be greater than or equal to start date.");
        }

        IReadOnlyList<OrderDto> orders = await _orderService.GetByDateRangeAsync(startDate, endDate, cancellationToken);
        return Ok(Response<IReadOnlyList<OrderDto>>.Success(orders, "Orders retrieved successfully"));
    }

    /// <summary>
    /// Creates a new order.
    /// </summary>
    /// <param name="createDto">The order creation data containing customer ID, order items, and other required fields.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created order with generated ID, or 400 Bad Request if validation fails.</returns>
    /// <remarks>
    /// This endpoint creates a new order in the system. The request body must contain all required fields as defined in <see cref="CreateOrderDto"/>,
    /// including a valid customer ID and at least one order item. The order total is calculated automatically based on the items.
    /// Upon successful creation, the response includes a Location header pointing to the new order resource.
    /// </remarks>
    /// <example>
    /// <code>
    /// POST /api/v1/orders
    /// {
    ///   "customerId": 123,
    ///   "items": [
    ///     {
    ///       "productId": 456,
    ///       "quantity": 2,
    ///       "price": 99.99
    ///     }
    ///   ]
    /// }
    /// </code>
    /// </example>
    [HttpPost]
    [ProducesResponseType(typeof(Response<OrderDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<OrderDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<OrderDto>>> Create([FromBody] CreateOrderDto createDto, CancellationToken cancellationToken)
    {
        OrderDto order = await _orderService.CreateAsync(createDto, cancellationToken);
        Response<OrderDto> response = Response<OrderDto>.Success(order, "Order created successfully");
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, response);
    }

    /// <summary>
    /// Updates an existing order (full update).
    /// </summary>
    /// <param name="id">The unique identifier of the order to update (must be greater than 0).</param>
    /// <param name="updateDto">The order update data containing all fields to update.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, 404 Not Found if order doesn't exist, or 400 Bad Request if validation fails.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<OrderDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<OrderDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateOrderDto updateDto, CancellationToken cancellationToken)
    {
        OrderDto? order = await _orderService.UpdateAsync(id, updateDto, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order not found for update. OrderId: {OrderId}", id);
            return HandleNotFound<OrderDto>("Order", "ID", id);
        }

        return NoContent();
    }

    /// <summary>
    /// Partially updates an order. Only properties present in the request body are updated; others are left unchanged.
    /// </summary>
    /// <param name="id">The unique identifier of the order to update (must be greater than 0).</param>
    /// <param name="patchDto">The update data; only provided properties are applied.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, or 404 Not Found if the order doesn't exist.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<OrderDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<OrderDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Patch(
        [FromRoute] int id,
        [FromBody] UpdateOrderDto patchDto,
        CancellationToken cancellationToken)
    {
        OrderDto? order = await _orderService.UpdateAsync(id, patchDto, cancellationToken);
        if (order == null)
        {
            _logger.LogWarning("Order not found for patch. OrderId: {OrderId}", id);
            return HandleNotFound<OrderDto>("Order", "ID", id);
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes an order (soft delete).
    /// </summary>
    /// <param name="id">The unique identifier of the order to delete (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, or 404 Not Found if order doesn't exist.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        bool deleted = await _orderService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning("Order not found for deletion. OrderId: {OrderId}", id);
            return HandleNotFound<object>("Order", "ID", id);
        }

        return NoContent();
    }

    /// <summary>
    /// Updates multiple orders in a batch operation.
    /// </summary>
    /// <param name="updates">List of order updates (ID and update data pairs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The updated orders.</returns>
    [HttpPut("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<OrderDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<OrderDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<OrderDto>>>> UpdateBatch([FromBody] IReadOnlyList<BatchUpdateRequest<UpdateOrderDto>> updates, CancellationToken cancellationToken)
    {
        IReadOnlyList<(int Id, UpdateOrderDto UpdateDto)> updateList = updates.Select(u => (u.Id, u.Data)).ToList();
        IReadOnlyList<OrderDto> orders = await _orderService.UpdateBatchAsync(updateList, cancellationToken);
        return Ok(Response<IReadOnlyList<OrderDto>>.Success(orders, "Orders updated successfully"));
    }

    /// <summary>
    /// Deletes multiple orders in a batch operation (soft delete).
    /// </summary>
    /// <param name="ids">List of order IDs to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>List of successfully deleted order IDs.</returns>
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<int>>>> DeleteBatch([FromBody] IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> deletedIds = await _orderService.DeleteBatchAsync(ids, cancellationToken);
        return Ok(Response<IReadOnlyList<int>>.Success(deletedIds, "Orders deleted successfully"));
    }
}

