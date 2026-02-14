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
/// Service implementation for stock business operations.
/// </summary>
public class StockService(
    IStockRepository stockRepository,
    IUnitOfWork unitOfWork,
    ILogger<StockService> logger)
    : CrudServiceBase<Stock, StockDto, CreateStockDto, UpdateStockDto>(stockRepository, unitOfWork, logger),
        Interfaces.IStockService
{
    /// <inheritdoc />
    protected override string EntityName => "Stock";

    /// <inheritdoc />
    protected override string EntityNamePlural => "Stock entries";

    /// <inheritdoc />
    public async Task<Result<StockDto>> GetByArticleIdAsync(int articleId, CancellationToken cancellationToken = default)
    {
        Stock? stock = await stockRepository.GetByArticleIdAsync(articleId, cancellationToken).ConfigureAwait(false);
        return stock == null ? Result<StockDto>.NotFound() : Result<StockDto>.Success(stock.Adapt<StockDto>());
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<StockDto>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
    {
        List<Stock> stock = await stockRepository.GetLowStockAsync(threshold, cancellationToken).ConfigureAwait(false);
        return stock.Adapt<IReadOnlyList<StockDto>>();
    }

    /// <inheritdoc />
    protected override bool ApplyPatch(Stock entity, UpdateStockDto patchDto)
    {
        return PartialUpdateHelper.ApplyIfChanged(entity.ArticleId, patchDto.ArticleId, v => entity.ArticleId = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Count, patchDto.Count, v => entity.Count = v);
    }
}
