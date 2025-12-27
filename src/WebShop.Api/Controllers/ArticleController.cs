using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Services.Interfaces;

namespace WebShop.Api.Controllers;

/// <summary>
/// Manages article resources.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ArticleController"/> class.
/// </remarks>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/articles")]
[Produces("application/json")]
public class ArticleController(IArticleService articleService, ILogger<ArticleController> logger) : BaseApiController
{
    private readonly IArticleService _articleService = articleService;
    private readonly ILogger<ArticleController> _logger = logger;

    /// <summary>
    /// Gets all articles.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of articles.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ArticleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<ArticleDto>>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<ArticleDto> articles = await _articleService.GetAllAsync(cancellationToken);
        return Ok(Response<IReadOnlyList<ArticleDto>>.Success(articles, "Articles retrieved successfully"));
    }

    /// <summary>
    /// Gets an article by ID.
    /// </summary>
    /// <param name="id">Article identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Article if found, otherwise 404.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(Response<ArticleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<ArticleDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<ArticleDto>>> GetById([FromRoute] int id, CancellationToken cancellationToken)
    {
        ArticleDto? article = await _articleService.GetByIdAsync(id, cancellationToken);
        if (article == null)
        {
            _logger.LogWarning("Article not found. ArticleId: {ArticleId}", id);
            return HandleNotFound<ArticleDto>("Article", "ID", id)
                ?? NotFoundResponse<ArticleDto>("Article not found", $"Article with ID {id} not found.");
        }

        return Ok(Response<ArticleDto>.Success(article, "Article retrieved successfully"));
    }

    /// <summary>
    /// Gets articles by product ID.
    /// </summary>
    /// <param name="productId">The unique identifier of the product (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A list of articles (variants) associated with the specified product.</returns>
    /// <remarks>
    /// This endpoint retrieves all articles (product variants) that belong to a specific product.
    /// Articles represent different variations of a product (e.g., different sizes, colors, or configurations).
    /// Returns an empty list if no articles are found for the product.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/articles/product/456
    /// </code>
    /// </example>
    [HttpGet("product/{productId}")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ArticleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<ArticleDto>>>> GetByProductId([FromRoute] int productId, CancellationToken cancellationToken)
    {
        IReadOnlyList<ArticleDto> articles = await _articleService.GetByProductIdAsync(productId, cancellationToken);
        return Ok(Response<IReadOnlyList<ArticleDto>>.Success(articles, "Articles retrieved successfully"));
    }

    /// <summary>
    /// Gets active articles only.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of active articles.</returns>
    [HttpGet("active")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ArticleDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<Response<IReadOnlyList<ArticleDto>>>> GetActive(CancellationToken cancellationToken)
    {
        IReadOnlyList<ArticleDto> articles = await _articleService.GetActiveArticlesAsync(cancellationToken);
        return Ok(Response<IReadOnlyList<ArticleDto>>.Success(articles, "Active articles retrieved successfully"));
    }

    /// <summary>
    /// Gets an article by EAN code.
    /// </summary>
    /// <param name="ean">The European Article Number (EAN) code, typically 13 digits.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The article if found, otherwise returns 404 Not Found.</returns>
    /// <remarks>
    /// This endpoint retrieves an article by its EAN (European Article Number) code. EAN codes are unique identifiers for products.
    /// The EAN code is typically 13 digits long and is used for barcode scanning and inventory management.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/articles/ean/1234567890123
    /// </code>
    /// </example>
    [HttpGet("ean/{ean}")]
    [ProducesResponseType(typeof(Response<ArticleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<ArticleDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<ArticleDto>>> GetByEan([FromRoute] string ean, CancellationToken cancellationToken)
    {
        ArticleDto? article = await _articleService.GetByEanAsync(ean, cancellationToken);
        if (article == null)
        {
            _logger.LogWarning("Article not found by EAN. EAN: {EAN}", ean);
            return NotFoundResponse<ArticleDto>("Article not found", $"Article with EAN {ean} not found.");
        }

        return Ok(Response<ArticleDto>.Success(article, "Article retrieved successfully"));
    }

    /// <summary>
    /// Creates a new article.
    /// </summary>
    /// <param name="createDto">The article creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created article with generated ID, or 400 Bad Request if validation fails.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Response<ArticleDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<ArticleDto>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<ArticleDto>>> Create([FromBody] CreateArticleDto createDto, CancellationToken cancellationToken)
    {
        ArticleDto article = await _articleService.CreateAsync(createDto, cancellationToken);
        Response<ArticleDto> response = Response<ArticleDto>.Success(article, "Article created successfully");
        return CreatedAtAction(nameof(GetById), new { id = article.Id }, response);
    }

    /// <summary>
    /// Updates an existing article (full update).
    /// </summary>
    /// <param name="id">The unique identifier of the article to update (must be greater than 0).</param>
    /// <param name="updateDto">The article update data containing all fields to update.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, 404 Not Found if article doesn't exist, or 400 Bad Request if validation fails.</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<ArticleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<ArticleDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update([FromRoute] int id, [FromBody] UpdateArticleDto updateDto, CancellationToken cancellationToken)
    {
        ArticleDto? article = await _articleService.UpdateAsync(id, updateDto, cancellationToken);
        if (article == null)
        {
            _logger.LogWarning("Article not found for update. ArticleId: {ArticleId}", id);
            return HandleNotFound<ArticleDto>("Article", "ID", id)
                ?? NotFoundResponse<ArticleDto>("Article not found", $"Article with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Partially updates an article using JSON Patch (RFC 6902).
    /// </summary>
    /// <param name="id">The unique identifier.</param>
    /// <param name="patchDto">The update data.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>204 No Content if successful, or 404 Not Found.</returns>
    [HttpPatch("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<ArticleDto>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(Response<ArticleDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Patch(
        [FromRoute] int id,
        [FromBody] UpdateArticleDto patchDto,
        CancellationToken cancellationToken)
    {
        ArticleDto? article = await _articleService.UpdateAsync(id, patchDto, cancellationToken);
        if (article == null)
        {
            _logger.LogWarning("Article not found for patch. ArticleId: {ArticleId}", id);
            return HandleNotFound<ArticleDto>("Article", "ID", id)
                ?? NotFoundResponse<ArticleDto>("Article not found", $"Article with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Deletes an article (soft delete).
    /// </summary>
    /// <param name="id">The unique identifier of the article to delete (must be greater than 0).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>204 No Content if successful, or 404 Not Found if article doesn't exist.</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(Response<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
    {
        bool deleted = await _articleService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            _logger.LogWarning("Article not found for deletion. ArticleId: {ArticleId}", id);
            return NotFoundResponse<object>("Article not found", $"Article with ID {id} not found.");
        }

        return NoContent();
    }

    /// <summary>
    /// Creates multiple articles in a batch operation.
    /// </summary>
    /// <param name="createDtos">List of article creation data.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The newly created articles with generated IDs.</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ArticleDto>>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ArticleDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<ArticleDto>>>> CreateBatch([FromBody] IReadOnlyList<CreateArticleDto> createDtos, CancellationToken cancellationToken)
    {
        IReadOnlyList<ArticleDto> articles = await _articleService.CreateBatchAsync(createDtos, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, Response<IReadOnlyList<ArticleDto>>.Success(articles, "Articles created successfully"));
    }

    /// <summary>
    /// Updates multiple articles in a batch operation.
    /// </summary>
    /// <param name="updates">List of article updates (ID and update data pairs).</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>The updated articles.</returns>
    [HttpPut("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ArticleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<ArticleDto>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<ArticleDto>>>> UpdateBatch([FromBody] IReadOnlyList<BatchUpdateRequest<UpdateArticleDto>> updates, CancellationToken cancellationToken)
    {
        IReadOnlyList<(int Id, UpdateArticleDto UpdateDto)> updateList = updates.Select(u => (u.Id, u.Data)).ToList();
        IReadOnlyList<ArticleDto> articles = await _articleService.UpdateBatchAsync(updateList, cancellationToken);
        return Ok(Response<IReadOnlyList<ArticleDto>>.Success(articles, "Articles updated successfully"));
    }

    /// <summary>
    /// Deletes multiple articles in a batch operation (soft delete).
    /// </summary>
    /// <param name="ids">List of article IDs to delete.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>List of successfully deleted article IDs.</returns>
    [HttpDelete("batch")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<int>>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Response<IReadOnlyList<int>>>> DeleteBatch([FromBody] IReadOnlyList<int> ids, CancellationToken cancellationToken)
    {
        IReadOnlyList<int> deletedIds = await _articleService.DeleteBatchAsync(ids, cancellationToken);
        return Ok(Response<IReadOnlyList<int>>.Success(deletedIds, "Articles deleted successfully"));
    }
}

