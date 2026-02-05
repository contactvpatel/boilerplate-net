using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for product business operations.
/// </summary>
public class ProductService(IProductRepository productRepository, ILogger<ProductService> logger) : Interfaces.IProductService
{
    private readonly IProductRepository _productRepository = productRepository
        ?? throw new ArgumentNullException(nameof(productRepository));
    private readonly ILogger<ProductService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<ProductDto?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        Product? product = await _productRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return product?.Adapt<ProductDto>();
    }

    public async Task<IReadOnlyList<ProductDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Product> products = await _productRepository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return products.Adapt<IReadOnlyList<ProductDto>>();
    }

    public async Task<(IReadOnlyList<ProductDto> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<Product> items, int totalCount) = await _productRepository
            .GetPagedAsync(pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<ProductDto> productDtos = items.Adapt<IReadOnlyList<ProductDto>>();
        return (productDtos, totalCount);
    }

    public async Task<ProductDto> CreateAsync(CreateProductDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto, nameof(createDto));

        _logger.LogInformation("Creating new product. Name: {Name}, Category: {Category}", createDto.Name, createDto.Category);
        Product product = createDto.Adapt<Product>();
        await _productRepository.AddAsync(product, cancellationToken).ConfigureAwait(false);
        await _productRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Product created successfully. ProductId: {ProductId}", product.Id);
        return product.Adapt<ProductDto>();
    }

    public async Task<ProductDto?> UpdateAsync(int id, UpdateProductDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto, nameof(updateDto));

        _logger.LogInformation("Updating product. ProductId: {ProductId}, Name: {Name}, Category: {Category}", id, updateDto.Name, updateDto.Category);
        Product? product = await _productRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (product == null)
        {
            return null;
        }

        updateDto.Adapt(product);
        await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        await _productRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Product updated successfully. ProductId: {ProductId}", id);
        return product.Adapt<ProductDto>();
    }

    public async Task<ProductDto?> PatchAsync(int id, UpdateProductDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto, nameof(patchDto));

        Product? product = await _productRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (product == null)
        {
            return null;
        }

        // Partial update: Only update fields that are provided (not null)
        bool hasChanges = false;

        if (patchDto.Name != null && product.Name != patchDto.Name)
        {
            product.Name = patchDto.Name;
            hasChanges = true;
        }

        if (patchDto.LabelId.HasValue && product.LabelId != patchDto.LabelId.Value)
        {
            product.LabelId = patchDto.LabelId.Value;
            hasChanges = true;
        }

        if (patchDto.Category != null && product.Category != patchDto.Category)
        {
            product.Category = patchDto.Category;
            hasChanges = true;
        }

        if (patchDto.Gender != null && product.Gender != patchDto.Gender)
        {
            product.Gender = patchDto.Gender;
            hasChanges = true;
        }

        if (patchDto.CurrentlyActive.HasValue && product.CurrentlyActive != patchDto.CurrentlyActive.Value)
        {
            product.CurrentlyActive = patchDto.CurrentlyActive.Value;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
            await _productRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Product patched successfully. ProductId: {ProductId}", id);
        }

        return product.Adapt<ProductDto>();
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Deleting product. ProductId: {ProductId}", id);

        // Check if entity exists (including soft-deleted) for idempotency
        bool exists = await _productRepository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        // Check if already soft-deleted
        Product? product = await _productRepository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (product == null)
        {
            // Already soft-deleted - return true for idempotency (controller will return 204)
            _logger.LogInformation("Product already deleted. ProductId: {ProductId}", id);
            return true;
        }

        // Perform soft delete
        await _productRepository.DeleteAsync(product, cancellationToken).ConfigureAwait(false);
        await _productRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Product deleted successfully. ProductId: {ProductId}", id);
        return true;
    }

    public async Task<IReadOnlyList<ProductDto>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        List<Product> products = await _productRepository.GetByCategoryAsync(category, cancellationToken).ConfigureAwait(false);
        return products.Adapt<IReadOnlyList<ProductDto>>();
    }

    public async Task<IReadOnlyList<ProductDto>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        List<Product> products = await _productRepository.GetActiveProductsAsync(cancellationToken).ConfigureAwait(false);
        return products.Adapt<IReadOnlyList<ProductDto>>();
    }

    public async Task<IReadOnlyList<ProductDto>> CreateBatchAsync(IReadOnlyList<CreateProductDto> createDtos, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos, nameof(createDtos));

        if (createDtos.Count == 0)
        {
            return Array.Empty<ProductDto>();
        }

        _logger.LogInformation("Creating {Count} products in batch", createDtos.Count);
        List<Product> products = createDtos.Select(dto => dto.Adapt<Product>()).ToList();

        foreach (Product product in products)
        {
            await _productRepository.AddAsync(product, cancellationToken).ConfigureAwait(false);
        }

        await _productRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch created {Count} products successfully", products.Count);
        return products.Adapt<IReadOnlyList<ProductDto>>();
    }

    public async Task<IReadOnlyList<ProductDto>> UpdateBatchAsync(IReadOnlyList<(int Id, UpdateProductDto UpdateDto)> updates, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates, nameof(updates));

        if (updates.Count == 0)
        {
            return Array.Empty<ProductDto>();
        }

        _logger.LogInformation("Updating {Count} products in batch", updates.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<Product> products = await _productRepository.FindAsync(p => ids.Contains(p.Id), cancellationToken).ConfigureAwait(false);

        // Create lookup dictionary for O(1) access
        Dictionary<int, Product> productLookup = products.ToDictionary(p => p.Id);

        List<ProductDto> updatedProducts = new(updates.Count);
        foreach ((int id, UpdateProductDto updateDto) in updates)
        {
            if (productLookup.TryGetValue(id, out Product? product))
            {
                updateDto.Adapt(product);
                await _productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
                updatedProducts.Add(product.Adapt<ProductDto>());
            }
        }

        await _productRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch updated {Count} products successfully", updatedProducts.Count);
        return updatedProducts;
    }

    public async Task<IReadOnlyList<int>> DeleteBatchAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids, nameof(ids));

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        _logger.LogInformation("Deleting {Count} products in batch", ids.Count);

        // Load all entities in a single query to avoid N+1 problem
        IReadOnlyList<Product> products = await _productRepository.FindAsync(p => ids.Contains(p.Id), cancellationToken).ConfigureAwait(false);

        List<int> deletedIds = new(products.Count);
        foreach (Product product in products)
        {
            await _productRepository.DeleteAsync(product, cancellationToken).ConfigureAwait(false);
            deletedIds.Add(product.Id);
        }

        await _productRepository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Batch deleted {Count} products successfully", deletedIds.Count);
        return deletedIds;
    }
}

