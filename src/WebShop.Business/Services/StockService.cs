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
/// Service implementation for stock business operations.
/// </summary>
public class StockService(IStockRepository stockRepository, IUnitOfWork unitOfWork, ILogger<StockService> logger) : Interfaces.IStockService
{
    public async Task<StockDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Stock? stock = await stockRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return stock?.Adapt<StockDto>();
    }

    public async Task<IReadOnlyList<StockDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Stock> stock = await stockRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return stock.Adapt<IReadOnlyList<StockDto>>();
    }

    public async Task<StockDto?> GetByArticleIdAsync(int articleId, CancellationToken cancellationToken = default)
    {
        Stock? stock = await stockRepository.GetByArticleIdAsync(articleId, cancellationToken).ConfigureAwait(false);
        return stock?.Adapt<StockDto>();
    }

    public async Task<IReadOnlyList<StockDto>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
    {
        List<Stock> stock = await stockRepository.GetLowStockAsync(threshold, cancellationToken).ConfigureAwait(false);
        return stock.Adapt<IReadOnlyList<StockDto>>();
    }

    public async Task<StockDto> CreateAsync(CreateStockDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        logger.LogInformation("Creating new stock entry. ArticleId: {ArticleId}, Count: {Count}", createDto.ArticleId, createDto.Count);
        Stock stock = createDto.Adapt<Stock>();
        await stockRepository.AddAsync(stock, cancellationToken).ConfigureAwait(false);
        await stockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Stock entry created successfully. StockId: {StockId}", stock.Id);
        return stock.Adapt<StockDto>();
    }

    public async Task<StockDto?> UpdateAsync(int id, UpdateStockDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        logger.LogInformation("Updating stock entry. StockId: {StockId}", id);
        Stock? stock = await stockRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (stock == null)
        {
            return null;
        }

        updateDto.Adapt(stock);
        await stockRepository.UpdateAsync(stock, cancellationToken).ConfigureAwait(false);
        await stockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Stock entry updated successfully. StockId: {StockId}", id);
        return stock.Adapt<StockDto>();
    }

    public async Task<StockDto?> PatchAsync(int id, UpdateStockDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto);

        Stock? stock = await stockRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (stock == null)
        {
            return null;
        }

        bool hasChanges = false;

        if (patchDto.ArticleId.HasValue && stock.ArticleId != patchDto.ArticleId.Value)
        {
            stock.ArticleId = patchDto.ArticleId.Value;
            hasChanges = true;
        }

        if (patchDto.Count.HasValue && stock.Count != patchDto.Count.Value)
        {
            stock.Count = patchDto.Count.Value;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await stockRepository.UpdateAsync(stock, cancellationToken).ConfigureAwait(false);
            await stockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Stock entry patched successfully. StockId: {StockId}", id);
        }

        return stock.Adapt<StockDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Deleting stock entry. StockId: {StockId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await stockRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        // Check if already soft-deleted
        Stock? stock = await stockRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (stock == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            logger.LogInformation("Stock entry already deleted. StockId: {StockId}", id);
            return true;
        }

        // Perform soft delete
        await stockRepository.DeleteAsync(stock, cancellationToken).ConfigureAwait(false);
        await stockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Stock entry deleted successfully. StockId: {StockId}", id);
        return true;
    }

    public async Task<IReadOnlyList<StockDto>> CreateBatchAsync(IReadOnlyList<CreateStockDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos);

        if (createDtos.Count == 0)
        {
            return Array.Empty<StockDto>();
        }

        logger.LogInformation("Creating {Count} stock entries in batch", createDtos.Count);
        List<Stock> stocks = createDtos.Select(dto => dto.Adapt<Stock>()).ToList();

        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Stock stock in stocks)
            {
                await stockRepository.AddAsync(stock, ct).ConfigureAwait(false);
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch created {Count} stock entries successfully", stocks.Count);
        return stocks.Adapt<IReadOnlyList<StockDto>>();
    }

    public async Task<IReadOnlyList<StockDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateStockDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        if (updates.Count == 0)
        {
            return Array.Empty<StockDto>();
        }

        logger.LogInformation("Updating {Count} stock entries in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Stock> stocks = await stockRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Stock> stockLookup = stocks.ToDictionary(s => s.Id);

        List<StockDto> updatedStocks = new(updates.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach ((int id, UpdateStockDto updateDto) in updates)
            {
                if (stockLookup.TryGetValue(id, out Stock? stock))
                {
                    updateDto.Adapt(stock);
                    await stockRepository.UpdateAsync(stock, ct).ConfigureAwait(false);
                    updatedStocks.Add(stock.Adapt<StockDto>());
                }
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch updated {Count} stock entries successfully", updatedStocks.Count);
        return updatedStocks;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        logger.LogInformation("Deleting {Count} stock entries in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Stock> stocks = await stockRepository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(stocks.Count);
        await BatchOperationHelper.ExecuteInTransactionAsync(unitOfWork, async ct =>
        {
            foreach (Stock stock in stocks)
            {
                await stockRepository.DeleteAsync(stock, ct).ConfigureAwait(false);
                deletedIds.Add(stock.Id);
            }
        }, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Batch deleted {Count} stock entries successfully", deletedIds.Count);
        return deletedIds;
    }
}

