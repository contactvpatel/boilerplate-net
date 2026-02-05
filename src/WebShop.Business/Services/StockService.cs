using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for stock business operations.
/// </summary>
public class StockService(IStockRepository stockRepository, ILogger<StockService> logger) : Interfaces.IStockService
{
    private readonly IStockRepository _stockRepository = stockRepository
        ?? throw new ArgumentNullException(nameof(stockRepository));
    private readonly ILogger<StockService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<StockDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Stock? stock = await _stockRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return stock?.Adapt<StockDto>();
    }

    public async Task<IReadOnlyList<StockDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Stock> stock = await _stockRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return stock.Adapt<IReadOnlyList<StockDto>>();
    }

    public async Task<StockDto?> GetByArticleIdAsync(int articleId, CancellationToken cancellationToken = default)
    {
        Stock? stock = await _stockRepository.GetByArticleIdAsync(articleId, cancellationToken).ConfigureAwait(false);
        return stock?.Adapt<StockDto>();
    }

    public async Task<IReadOnlyList<StockDto>> GetLowStockAsync(int threshold, CancellationToken cancellationToken = default)
    {
        List<Stock> stock = await _stockRepository.GetLowStockAsync(threshold, cancellationToken).ConfigureAwait(false);
        return stock.Adapt<IReadOnlyList<StockDto>>();
    }

    public async Task<StockDto> CreateAsync(CreateStockDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        _logger.LogInformation("Creating new stock entry. ArticleId: {ArticleId}, Count: {Count}", createDto.ArticleId, createDto.Count);
        Stock stock = createDto.Adapt<Stock>();
        await _stockRepository.AddAsync(stock, cancellationToken).ConfigureAwait(false);
        await _stockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Stock entry created successfully. StockId: {StockId}", stock.Id);
        return stock.Adapt<StockDto>();
    }

    public async Task<StockDto?> UpdateAsync(int id, UpdateStockDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        _logger.LogInformation("Updating stock entry. StockId: {StockId}", id);
        Stock? stock = await _stockRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (stock == null)
        {
            return null;
        }

        updateDto.Adapt(stock);
        await _stockRepository.UpdateAsync(stock, cancellationToken).ConfigureAwait(false);
        await _stockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Stock entry updated successfully. StockId: {StockId}", id);
        return stock.Adapt<StockDto>();
    }

    public async Task<StockDto?> PatchAsync(int id, UpdateStockDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto, nameof(patchDto));

        Stock? stock = await _stockRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
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
            await _stockRepository.UpdateAsync(stock, cancellationToken).ConfigureAwait(false);
            await _stockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Stock entry patched successfully. StockId: {StockId}", id);
        }

        return stock.Adapt<StockDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting stock entry. StockId: {StockId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await _stockRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        // Check if already soft-deleted
        Stock? stock = await _stockRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (stock == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            _logger.LogInformation("Stock entry already deleted. StockId: {StockId}", id);
            return true;
        }

        // Perform soft delete
        await _stockRepository.DeleteAsync(stock, cancellationToken).ConfigureAwait(false);
        await _stockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Stock entry deleted successfully. StockId: {StockId}", id);
        return true;
    }

    public async Task<IReadOnlyList<StockDto>> CreateBatchAsync(IReadOnlyList<CreateStockDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos, nameof(createDtos));

        if (createDtos.Count == 0)
        {
            return Array.Empty<StockDto>();
        }

        _logger.LogInformation("Creating {Count} stock entries in batch", createDtos.Count);
        List<Stock> stocks = createDtos.Select(dto => dto.Adapt<Stock>()).ToList();

        foreach (Stock stock in stocks)
        {
            await _stockRepository.AddAsync(stock, cancellationToken).ConfigureAwait(false);
        }

        await _stockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch created {Count} stock entries successfully", stocks.Count);
        return stocks.Adapt<IReadOnlyList<StockDto>>();
    }

    public async Task<IReadOnlyList<StockDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateStockDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates, nameof(updates));

        if (updates.Count == 0)
        {
            return Array.Empty<StockDto>();
        }

        _logger.LogInformation("Updating {Count} stock entries in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Stock> stocks = await _stockRepository.FindAsync(s => ids.Contains(s.Id), cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Stock> stockLookup = stocks.ToDictionary(s => s.Id);

        List<StockDto> updatedStocks = new(updates.Count);
        foreach ((int id, UpdateStockDto updateDto) in updates)
        {
            if (stockLookup.TryGetValue(id, out Stock? stock))
            {
                updateDto.Adapt(stock);
                await _stockRepository.UpdateAsync(stock, cancellationToken).ConfigureAwait(false);
                updatedStocks.Add(stock.Adapt<StockDto>());
            }
        }

        await _stockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch updated {Count} stock entries successfully", updatedStocks.Count);
        return updatedStocks;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        _logger.LogInformation("Deleting {Count} stock entries in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Stock> stocks = await _stockRepository.FindAsync(s => ids.Contains(s.Id), cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(stocks.Count);
        foreach (Stock stock in stocks)
        {
            await _stockRepository.DeleteAsync(stock, cancellationToken).ConfigureAwait(false);
            deletedIds.Add(stock.Id);
        }

        await _stockRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch deleted {Count} stock entries successfully", deletedIds.Count);
        return deletedIds;
    }
}

