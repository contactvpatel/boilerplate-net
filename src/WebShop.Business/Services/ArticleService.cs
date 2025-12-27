using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for article business operations.
/// </summary>
public class ArticleService(IArticleRepository articleRepository, ILogger<ArticleService> logger) : Interfaces.IArticleService
{
    private readonly IArticleRepository _articleRepository = articleRepository
        ?? throw new ArgumentNullException(nameof(articleRepository));
    private readonly ILogger<ArticleService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<ArticleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Article? article = await _articleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return article?.Adapt<ArticleDto>();
    }

    public async Task<IReadOnlyList<ArticleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Article> articles = await _articleRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return articles.Adapt<IReadOnlyList<ArticleDto>>();
    }

    public async Task<IReadOnlyList<ArticleDto>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        List<Article> articles = await _articleRepository.GetByProductIdAsync(productId, cancellationToken).ConfigureAwait(false);
        return articles.Adapt<IReadOnlyList<ArticleDto>>();
    }

    public async Task<IReadOnlyList<ArticleDto>> GetActiveArticlesAsync(CancellationToken cancellationToken = default)
    {
        List<Article> articles = await _articleRepository.GetActiveArticlesAsync(cancellationToken).ConfigureAwait(false);
        return articles.Adapt<IReadOnlyList<ArticleDto>>();
    }

    public async Task<ArticleDto?> GetByEanAsync(string ean, CancellationToken cancellationToken = default)
    {
        Article? article = await _articleRepository.GetByEanAsync(ean, cancellationToken).ConfigureAwait(false);
        return article?.Adapt<ArticleDto>();
    }

    public async Task<ArticleDto> CreateAsync(CreateArticleDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        _logger.LogInformation("Creating new article. ProductId: {ProductId}, Ean: {Ean}", createDto.ProductId, createDto.Ean);
        Article article = createDto.Adapt<Article>();
        await _articleRepository.AddAsync(article, cancellationToken).ConfigureAwait(false);
        await _articleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Article created successfully. ArticleId: {ArticleId}", article.Id);
        return article.Adapt<ArticleDto>();
    }

    public async Task<ArticleDto?> UpdateAsync(int id, UpdateArticleDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        _logger.LogInformation("Updating article. ArticleId: {ArticleId}", id);
        Article? article = await _articleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (article == null)
        {
            _logger.LogWarning("Article not found for update. ArticleId: {ArticleId}", id);
            return null;
        }

        // TODO: Map update properties
        await _articleRepository.UpdateAsync(article, cancellationToken).ConfigureAwait(false);
        await _articleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Article updated successfully. ArticleId: {ArticleId}", id);
        return article.Adapt<ArticleDto>();
    }

    public async Task<ArticleDto?> PatchAsync(int id, UpdateArticleDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto, nameof(patchDto));

        Article? article = await _articleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (article == null)
        {
            _logger.LogWarning("Article not found for patch. ArticleId: {ArticleId}", id);
            return null;
        }

        bool hasChanges = false;

        if (patchDto.ProductId.HasValue && article.ProductId != patchDto.ProductId.Value)
        {
            article.ProductId = patchDto.ProductId.Value;
            hasChanges = true;
        }

        if (patchDto.Ean != null && article.Ean != patchDto.Ean)
        {
            article.Ean = patchDto.Ean;
            hasChanges = true;
        }

        if (patchDto.ColorId.HasValue && article.ColorId != patchDto.ColorId.Value)
        {
            article.ColorId = patchDto.ColorId.Value;
            hasChanges = true;
        }

        if (patchDto.Size.HasValue && article.Size != patchDto.Size.Value)
        {
            article.Size = patchDto.Size.Value;
            hasChanges = true;
        }

        if (patchDto.Description != null && article.Description != patchDto.Description)
        {
            article.Description = patchDto.Description;
            hasChanges = true;
        }

        if (patchDto.OriginalPrice.HasValue && article.OriginalPrice != patchDto.OriginalPrice.Value)
        {
            article.OriginalPrice = patchDto.OriginalPrice.Value;
            hasChanges = true;
        }

        if (patchDto.ReducedPrice.HasValue && article.ReducedPrice != patchDto.ReducedPrice.Value)
        {
            article.ReducedPrice = patchDto.ReducedPrice.Value;
            hasChanges = true;
        }

        if (patchDto.TaxRate.HasValue && article.TaxRate != patchDto.TaxRate.Value)
        {
            article.TaxRate = patchDto.TaxRate.Value;
            hasChanges = true;
        }

        if (patchDto.DiscountInPercent.HasValue && article.DiscountInPercent != patchDto.DiscountInPercent.Value)
        {
            article.DiscountInPercent = patchDto.DiscountInPercent.Value;
            hasChanges = true;
        }

        if (patchDto.CurrentlyActive.HasValue && article.CurrentlyActive != patchDto.CurrentlyActive.Value)
        {
            article.CurrentlyActive = patchDto.CurrentlyActive.Value;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _articleRepository.UpdateAsync(article, cancellationToken).ConfigureAwait(false);
            await _articleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Article patched successfully. ArticleId: {ArticleId}", id);
        }

        return article.Adapt<ArticleDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting article. ArticleId: {ArticleId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await _articleRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            // Never existed - return false (controller will return 404)
            _logger.LogWarning("Article not found for deletion. ArticleId: {ArticleId}", id);
            return false;
        }

        // Check if already soft-deleted
        Article? article = await _articleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (article == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            _logger.LogInformation("Article already deleted. ArticleId: {ArticleId}", id);
            return true;
        }

        // Perform soft delete
        await _articleRepository.DeleteAsync(article, cancellationToken).ConfigureAwait(false);
        await _articleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Article deleted successfully. ArticleId: {ArticleId}", id);
        return true;
    }

    public async Task<IReadOnlyList<ArticleDto>> CreateBatchAsync(IReadOnlyList<CreateArticleDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos, nameof(createDtos));

        if (createDtos.Count == 0)
        {
            return Array.Empty<ArticleDto>();
        }

        _logger.LogInformation("Creating {Count} articles in batch", createDtos.Count);
        List<Article> articles = createDtos.Select(dto => dto.Adapt<Article>()).ToList();

        foreach (Article article in articles)
        {
            await _articleRepository.AddAsync(article, cancellationToken).ConfigureAwait(false);
        }

        await _articleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch created {Count} articles successfully", articles.Count);
        return articles.Adapt<IReadOnlyList<ArticleDto>>();
    }

    public async Task<IReadOnlyList<ArticleDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateArticleDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates, nameof(updates));

        if (updates.Count == 0)
        {
            return Array.Empty<ArticleDto>();
        }

        _logger.LogInformation("Updating {Count} articles in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Article> articles = await _articleRepository.FindAsync(a => ids.Contains(a.Id), cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Article> articleLookup = articles.ToDictionary(a => a.Id);

        List<ArticleDto> updatedArticles = new(updates.Count);
        foreach ((int id, UpdateArticleDto updateDto) in updates)
        {
            if (articleLookup.TryGetValue(id, out Article? article))
            {
                // TODO: Map update properties
                await _articleRepository.UpdateAsync(article, cancellationToken).ConfigureAwait(false);
                updatedArticles.Add(article.Adapt<ArticleDto>());
            }
        }

        await _articleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch updated {Count} articles successfully", updatedArticles.Count);
        return updatedArticles;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        _logger.LogInformation("Deleting {Count} articles in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Article> articles = await _articleRepository.FindAsync(a => ids.Contains(a.Id), cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(articles.Count);
        foreach (Article article in articles)
        {
            await _articleRepository.DeleteAsync(article, cancellationToken).ConfigureAwait(false);
            deletedIds.Add(article.Id);
        }

        await _articleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch deleted {Count} articles successfully", deletedIds.Count);
        return deletedIds;
    }
}

