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
/// Service implementation for color business operations.
/// </summary>
public class ColorService(
    IColorRepository colorRepository,
    IUnitOfWork unitOfWork,
    ILogger<ColorService> logger)
    : CrudServiceBase<Color, ColorDto, CreateColorDto, UpdateColorDto>(colorRepository, unitOfWork, logger),
        Interfaces.IColorService
{
    /// <inheritdoc />
    protected override string EntityName => "Color";

    /// <inheritdoc />
    public async Task<Result<ColorDto>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        Color? color = await colorRepository.GetByNameAsync(name, cancellationToken).ConfigureAwait(false);
        return color == null ? Result<ColorDto>.NotFound() : Result<ColorDto>.Success(color.Adapt<ColorDto>());
    }

    /// <inheritdoc />
    protected override bool ApplyPatch(Color entity, UpdateColorDto patchDto)
    {
        return PartialUpdateHelper.ApplyIfChanged(entity.Name, patchDto.Name, v => entity.Name = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.Rgb, patchDto.Rgb, v => entity.Rgb = v);
    }
}
