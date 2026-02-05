using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;

namespace WebShop.Api.Controllers;

/// <summary>
/// Manages product resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ProductController"/> class.
/// </remarks>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/products")]
[Produces("application/json")]
public class ProductController(IProductService productService, ILogger<ProductController> logger) : BaseApiController
{
    private readonly IProductService _productService = productService;
    private readonly ILogger<ProductController> _logger = logger;

    /// <summary>
    /// Gets all products with optional pagination.
    /// </summary>
    /// <param name="pagination">Pagination parameters (page, pageSize). Use page=0 or omit for non-paginated results.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A paginated list of products or all products if pagination is not requested.</returns>
    /// <remarks>
    /// <para><strong>Pagination (Recommended for large datasets)</strong></para>
    /// <para>Use query parameters: <c>?page=1&amp;pageSize=20</c></para>
    /// <para></para>
    /// <para><strong>Non-Paginated (Legacy)</strong></para>
    /// <para>Omit pagination parameters to get all products.</para>
    /// <para><strong>Warning:</strong> May cause performance issues with large datasets.</para>
    /// </remarks>
    [HttpGet]
    [ProducesResponseType(typeof(Response<PagedResult<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] PaginationQuery pagination,
        CancellationToken cancellationToken = default)
    {
        if (!pagination.IsPaginated)
        {
            IReadOnlyList<ProductDto> allProducts = await _productService.GetAllAsync(cancellationToken);
            return Ok(Response<IReadOnlyList<ProductDto>>.Success(allProducts, "Products retrieved successfully"));
        }

        (IReadOnlyList<ProductDto> items, int totalCount) = await _productService.GetPagedAsync(pagination.Page, pagination.PageSize, cancellationToken);
        PagedResult<ProductDto> pagedResult = new(items, pagination.Page, pagination.PageSize, totalCount);

        return Ok(Response<PagedResult<ProductDto>>.Success(
            pagedResult,
            $"Retrieved page {pagination.Page} of {pagedResult.TotalPages} ({items.Count} of {totalCount} total products)"));
    }

    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the product (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The product if found, otherwise returns 404 Not Found.</returns>
    /// <remarks>
    /// This endpoint retrieves a single product by its unique identifier. The ID must be a positive integer.
    /// This endpoint is cached for 1 minute as product data may change more frequently than reference data.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/products/456
    /// </code>
    /// </example>
    [HttpGet("{id}")]
    [ResponseCache(Duration = 60, Location = ResponseCacheLocation.Any, VaryByHeader = "Accept,Accept-Encoding")]
    [ProducesResponseType(typeof(Response<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<ProductDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<ProductDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        ProductDto? product = await _productService.GetByIdAsync(id, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found. ProductId: {ProductId}", id);
            return HandleNotFound<ProductDto>("Product", "ID", id);
        }

        return Ok(Response<ProductDto>.Success(product, "Product retrieved successfully"));
    }

    /// <summary>
    /// Gets products by category.
    /// </summary>
    /// <param name="category">The product category name (case-insensitive).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A list of products in the specified category.</returns>
    /// <remarks>
    /// This endpoint retrieves all products that belong to the specified category. Category matching is case-insensitive.
    /// Returns an empty list if no products are found in the category.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/products/category/Electronics
    /// </code>
    /// </example>
    [HttpGet("category/{category}")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<ProductDto>>>> GetByCategory([FromRoute] string category, CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductDto> products = await _productService.GetByCategoryAsync(category, cancellationToken);
        return Ok(Response<IReadOnlyList<ProductDto>>.Success(products, "Products retrieved successfully"));
    }

    /// <summary>
    /// Gets active products only.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A list of all active (non-deleted, available) products.</returns>
    /// <remarks>
    /// This endpoint retrieves only products that are currently active and available for sale.
    /// Inactive, deleted, or discontinued products are excluded from the results.
    /// </remarks>
    [HttpGet("active")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ProductDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<ProductDto>>>> GetActive(CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductDto> products = await _productService.GetActiveProductsAsync(cancellationToken);
        return Ok(Response<IReadOnlyList<ProductDto>>.Success(products, "Active products retrieved successfully"));
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="createDto">The product creation data containing name, category, price, and other required fields.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created product with generated ID, or 400 Bad Request if validation fails.</returns>
    /// <remarks>
    /// This endpoint creates a new product in the system. The request body must contain all required fields as defined in <see cref="CreateProductDto"/>.
    /// Upon successful creation, the response includes a Location header pointing to the new product resource.
    /// </remarks>
    /// <example>
    /// <code>
    /// POST /api/v1/products
    /// {
    ///   "name": "Laptop Computer",
    ///   "category": "Electronics",
    ///   "price": 999.99,
    ///   "description": "High-performance laptop"
    /// }
    /// </code>
    /// </example>
    [HttpPost]
    [ProducesResponseType(typeof(Response<ProductDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<ProductDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<ProductDto>>> Create([FromBody] CreateProductDto createDto, CancellationToken cancellationToken)
    {
        ProductDto product = await _productService.CreateAsync(createDto, cancellationToken);
        Response<ProductDto> response = Response<ProductDto>.Success(product, "Product created successfully");
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, response);
    }

    /// <summary>
    /// Updates an existing product.
    /// </summary>
    /// <param name="id">The unique identifier of the product to update (must be greater than 0).</param>
    /// <param name="updateDto">The product update data containing fields to modify.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, 404 Not Found if product doesn't exist, or 400 Bad Request if validation fails.</returns>
    /// <remarks>
    /// This endpoint performs a full update of the product. All fields in <see cref="UpdateProductDto"/> should be provided.
    /// This is a PUT operation, so partial updates are not supported.
    /// </remarks>
    /// <example>
    /// <code>
    /// PUT /api/v1/products/456
    /// {
    ///   "name": "Updated Laptop Computer",
    ///   "category": "Electronics",
    ///   "price": 899.99,
    ///   "description": "Updated description"
    /// }
    /// </code>
    /// </example>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<ProductDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<ProductDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateProductDto updateDto, CancellationToken cancellationToken)
    {
        ProductDto? product = await _productService.UpdateAsync(id, updateDto, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found for update. ProductId: {ProductId}", id);
            return HandleNotFound<ProductDto>("Product", "ID", id);
        }

        return NoContent();
    }

    /// <summary>
    /// Partially updates a product using JSON Patch (RFC 6902).
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="patchDto">The update data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>204 No Content if successful, or 404 Not Found.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<ProductDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<ProductDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Patch(
        [FromRoute] int id,
        [FromBody] UpdateProductDto patchDto,
        CancellationToken cancellationToken)
    {
        ProductDto? product = await _productService.UpdateAsync(id, patchDto, cancellationToken);
        if (product == null)
        {
            _logger.LogWarning("Product not found for patch. ProductId: {ProductId}", id);
            return HandleNotFound<ProductDto>("Product", "ID", id);
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes a product (soft delete).
    /// </summary>
    /// <param name="id">The unique identifier of the product to delete (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, or 404 Not Found if product doesn't exist.</returns>
    /// <remarks>
    /// This endpoint performs a soft delete on the product. The product record is marked as deleted but not physically removed from the database.
    /// Soft-deleted products are excluded from normal queries but can be recovered if needed.
    /// </remarks>
    /// <example>
    /// <code>
    /// DELETE /api/v1/products/456
    /// </code>
    /// </example>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        bool deleted = await _productService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning("Product not found for deletion. ProductId: {ProductId}", id);
            return HandleNotFound<object>("Product", "ID", id);
        }

        return NoContent();
    }

    /// <summary>
    /// Creates multiple products in a batch operation.
    /// </summary>
    /// <param name="createDtos">List of product creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created products with generated IDs.</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ProductDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ProductDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<ProductDto>>>> CreateBatch([FromBody] IReadOnlyList<CreateProductDto> createDtos, CancellationToken cancellationToken)
    {
        IReadOnlyList<ProductDto> products = await _productService.CreateBatchAsync(createDtos, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, Response<IReadOnlyList<ProductDto>>.Success(products, "Products created successfully"));
    }

    /// <summary>
    /// Updates multiple products in a batch operation.
    /// </summary>
    /// <param name="updates">List of product updates (ID and update data pairs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The updated products.</returns>
    [HttpPut("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ProductDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ProductDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<ProductDto>>>> UpdateBatch([FromBody] IReadOnlyList<BatchUpdateRequest<UpdateProductDto>> updates, CancellationToken cancellationToken)
    {
        IReadOnlyList<(int Id, UpdateProductDto UpdateDto)> updateList = updates.Select(u => (u.Id, u.Data)).ToList();
        IReadOnlyList<ProductDto> products = await _productService.UpdateBatchAsync(updateList, cancellationToken);
        return Ok(Response<IReadOnlyList<ProductDto>>.Success(products, "Products updated successfully"));
    }

    /// <summary>
    /// Deletes multiple products in a batch operation (soft delete).
    /// </summary>
    /// <param name="ids">List of product IDs to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>List of successfully deleted product IDs.</returns>
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<int>>>> DeleteBatch([FromBody] IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> deletedIds = await _productService.DeleteBatchAsync(ids, cancellationToken);
        return Ok(Response<IReadOnlyList<int>>.Success(deletedIds, "Products deleted successfully"));
    }
}

