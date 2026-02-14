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
/// Service implementation for product business operations.
/// </summary>
public class ProductService(
    IProductRepository productRepository,
    IUnitOfWork unitOfWork,
    ILogger<ProductService> logger)
    : CrudServiceBase<Product, ProductDto, CreateProductDto, UpdateProductDto>(productRepository, unitOfWork, logger),
        Interfaces.IProductService
{
    /// <inheritdoc />
    protected override string EntityName => "Product";

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductDto>> GetByCategoryAsync(string category, CancellationToken cancellationToken = default)
    {
        List<Product> products = await productRepository.GetByCategoryAsync(category, cancellationToken).ConfigureAwait(false);
        return products.Adapt<IReadOnlyList<ProductDto>>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ProductDto>> GetActiveProductsAsync(CancellationToken cancellationToken = default)
    {
        List<Product> products = await productRepository.GetActiveProductsAsync(cancellationToken).ConfigureAwait(false);
        return products.Adapt<IReadOnlyList<ProductDto>>();
    }

    /// <inheritdoc />
    protected override bool ApplyPatch(Product entity, UpdateProductDto patchDto)
    {
        return PartialUpdateHelper.ApplyIfChanged(entity.Name, patchDto.Name, v => entity.Name = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.LabelId, patchDto.LabelId, v => entity.LabelId = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Category, patchDto.Category, v => entity.Category = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Gender, patchDto.Gender, v => entity.Gender = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.CurrentlyActive, patchDto.CurrentlyActive, v => entity.CurrentlyActive = v);
    }
}
