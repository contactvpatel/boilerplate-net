using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Business.Helpers;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for article business operations.
/// </summary>
public class ArticleService(IArticleRepository articleRepository, IUnitOfWork unitOfWork, ILogger<ArticleService> logger) : Interfaces.IArticleService
{
    public async Task<ArticleDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Article? article = await articleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return article?.Adapt<ArticleDto>();
    }

    public async Task<IReadOnlyList<ArticleDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Article> articles = await articleRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return articles.Adapt<IReadOnlyList<ArticleDto>>();
    }

    public async Task<IReadOnlyList<ArticleDto>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        List<Article> articles = await articleRepository.GetByProductIdAsync(productId, cancellationToken).ConfigureAwait(false);
        return articles.Adapt<IReadOnlyList<ArticleDto>>();
    }

    public async Task<IReadOnlyList<ArticleDto>> GetActiveArticlesAsync(CancellationToken cancellationToken = default)
    {
        List<Article> articles = await articleRepository.GetActiveArticlesAsync(cancellationToken).ConfigureAwait(false);
        return articles.Adapt<IReadOnlyList<ArticleDto>>();
    }

    public async Task<ArticleDto?> GetByEanAsync(string ean, CancellationToken cancellationToken = default)
    {
        Article? article = await articleRepository.GetByEanAsync(ean, cancellationToken).ConfigureAwait(false);
        return article?.Adapt<ArticleDto>();
    }

    public async Task<ArticleDto> CreateAsync(CreateArticleDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        logger.LogInformation("Creating new article. ProductId: {ProductId}, Ean: {Ean}", createDto.ProductId, createDto.Ean);
        Article article = createDto.Adapt<Article>();
        await articleRepository.AddAsync(article, cancellationToken).ConfigureAwait(false);
        await articleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Article created successfully. ArticleId: {ArticleId}", article.Id);
        return article.Adapt<ArticleDto>();
    }

    public async Task<ArticleDto?> UpdateAsync(int id, UpdateArticleDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        logger.LogInformation("Updating article. ArticleId: {ArticleId}", id);
        Article? article = await articleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (article == null)
        {
            return null;
        }

        updateDto.Adapt(article);
        await articleRepository.UpdateAsync(article, cancellationToken).ConfigureAwait(false);
        await articleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Article updated successfully. ArticleId: {ArticleId}", id);
        return article.Adapt<ArticleDto>();
    }

    public async Task<ArticleDto?> PatchAsync(int id, UpdateArticleDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto);

        Article? article = await articleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (article == null)
        {
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
            await articleRepository.UpdateAsync(article, cancellationToken).ConfigureAwait(false);
            await articleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Article patched successfully. ArticleId: {ArticleId}", id);
        }

        return article.Adapt<ArticleDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting article. ArticleId: {ArticleId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await articleRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        // Check if already soft-deleted
        Article? article = await articleRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (article == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            logger.LogInformation("Article already deleted. ArticleId: {ArticleId}", id);
            return true;
        }

        // Perform soft delete
        await articleRepository.DeleteAsync(article, cancellationToken).ConfigureAwait(false);
        await articleRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Article deleted successfully. ArticleId: {ArticleId}", id);
        return true;
    }

    public async Task<IReadOnlyList<ArticleDto>> CreateBatchAsync(IReadOnlyList<CreateArticleDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos);

        if (createDtos.Count == 0)
        {
            return Array.Empty<ArticleDto>();
        }

        logger.LogInformation("Creating {Count} articles in batch", createDtos.Count);
        List<Article> articles = createDtos.Select(dto => dto.Adapt<Article>()).ToList();

        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Article article in articles)
            {
                await articleRepository.AddAsync(article, ct).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch created {Count} articles successfully", articles.Count);
        return articles.Adapt<IReadOnlyList<ArticleDto>>();
    }

    public async Task<IReadOnlyList<ArticleDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateArticleDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        if (updates.Count == 0)
        {
            return Array.Empty<ArticleDto>();
        }

        logger.LogInformation("Updating {Count} articles in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Article> articles = await articleRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Article> articleLookup = articles.ToDictionary(a => a.Id);

        List<ArticleDto> updatedArticles = new(updates.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach ((int id, UpdateArticleDto updateDto) in updates)
            {
                if (articleLookup.TryGetValue(id, out Article? article))
                {
                    updateDto.Adapt(article);
                    await articleRepository.UpdateAsync(article, ct).ConfigureAwait(false);
                    updatedArticles.Add(article.Adapt<ArticleDto>());
                }
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch updated {Count} articles successfully", updatedArticles.Count);
        return updatedArticles;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        logger.LogInformation("Deleting {Count} articles in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Article> articles = await articleRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(articles.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Article article in articles)
            {
                await articleRepository.DeleteAsync(article, ct).ConfigureAwait(false);
                deletedIds.Add(article.Id);
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch deleted {Count} articles successfully", deletedIds.Count);
        return deletedIds;
    }
}

