using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Business.Helpers;
using WebShop.Business.Models;
using WebShop.Business.Services.Base;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for article business operations.
/// </summary>
public class ArticleService(
    IArticleRepository articleRepository,
    IUnitOfWork unitOfWork,
    ILogger<ArticleService> logger)
    : CrudServiceBase<Article, ArticleDto, CreateArticleDto, UpdateArticleDto>(articleRepository, unitOfWork, logger),
        Interfaces.IArticleService
{
    /// <inheritdoc />
    protected override string EntityName => "Article";

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArticleDto>> GetByProductIdAsync(int productId, CancellationToken cancellationToken = default)
    {
        List<Article> articles = await articleRepository.GetByProductIdAsync(productId, cancellationToken).ConfigureAwait(false);
        return articles.Adapt<IReadOnlyList<ArticleDto>>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArticleDto>> GetActiveArticlesAsync(CancellationToken cancellationToken = default)
    {
        List<Article> articles = await articleRepository.GetActiveArticlesAsync(cancellationToken).ConfigureAwait(false);
        return articles.Adapt<IReadOnlyList<ArticleDto>>();
    }

    /// <inheritdoc />
    public async Task<Result<ArticleDto>> GetByEanAsync(string ean, CancellationToken cancellationToken = default)
    {
        Article? article = await articleRepository.GetByEanAsync(ean, cancellationToken).ConfigureAwait(false);
        return article == null ? Result<ArticleDto>.NotFound() : Result<ArticleDto>.Success(article.Adapt<ArticleDto>());
    }

    /// <inheritdoc />
    protected override bool ApplyPatch(Article entity, UpdateArticleDto patchDto)
    {
        return PartialUpdateHelper.ApplyIfChanged(entity.ProductId, patchDto.ProductId, v => entity.ProductId = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Ean, patchDto.Ean, v => entity.Ean = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.ColorId, patchDto.ColorId, v => entity.ColorId = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Size, patchDto.Size, v => entity.Size = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Description, patchDto.Description, v => entity.Description = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.OriginalPrice, patchDto.OriginalPrice, v => entity.OriginalPrice = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.ReducedPrice, patchDto.ReducedPrice, v => entity.ReducedPrice = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.TaxRate, patchDto.TaxRate, v => entity.TaxRate = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.DiscountInPercent, patchDto.DiscountInPercent, v => entity.DiscountInPercent = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.CurrentlyActive, patchDto.CurrentlyActive, v => entity.CurrentlyActive = v);
    }
}
