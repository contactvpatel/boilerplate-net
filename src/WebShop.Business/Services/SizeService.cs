using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Business.Helpers;
using WebShop.Business.Services.Base;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Business.Services;

/// <summary>
/// Service implementation for size business operations.
/// </summary>
public class SizeService(
    ISizeRepository sizeRepository,
    IUnitOfWork unitOfWork,
    ILogger<SizeService> logger)
    : CrudServiceBase<Size, SizeDto, CreateSizeDto, UpdateSizeDto>(sizeRepository, unitOfWork, logger),
        Interfaces.ISizeService
{
    /// <inheritdoc />
    protected override string EntityName => "Size";

    /// <inheritdoc />
    public async Task<IReadOnlyList<SizeDto>> GetByGenderAndCategoryAsync(string gender, string category, CancellationToken cancellationToken = default)
    {
        List<Size> sizes = await sizeRepository.GetByGenderAndCategoryAsync(gender, category, cancellationToken).ConfigureAwait(false);
        return sizes.Adapt<IReadOnlyList<SizeDto>>();
    }

    /// <inheritdoc />
    protected override bool ApplyPatch(Size entity, UpdateSizeDto patchDto)
    {
        return PartialUpdateHelper.ApplyIfChanged(entity.Gender, patchDto.Gender, v => entity.Gender = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Category, patchDto.Category, v => entity.Category = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.SizeLabel, patchDto.SizeLabel, v => entity.SizeLabel = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.SizeUs, patchDto.SizeUs, v => entity.SizeUs = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.SizeUk, patchDto.SizeUk, v => entity.SizeUk = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.SizeEu, patchDto.SizeEu, v => entity.SizeEu = v);
    }
}
