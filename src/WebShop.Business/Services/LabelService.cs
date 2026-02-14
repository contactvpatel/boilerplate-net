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
/// Service implementation for label business operations.
/// </summary>
public class LabelService(
    ILabelRepository labelRepository,
    IUnitOfWork unitOfWork,
    ILogger<LabelService> logger)
    : CrudServiceBase<Label, LabelDto, CreateLabelDto, UpdateLabelDto>(labelRepository, unitOfWork, logger),
        Interfaces.ILabelService
{
    /// <inheritdoc />
    protected override string EntityName => "Label";

    /// <inheritdoc />
    public async Task<Result<LabelDto>> GetBySlugNameAsync(string slugName, CancellationToken cancellationToken = default)
    {
        Label? label = await labelRepository.GetBySlugNameAsync(slugName, cancellationToken).ConfigureAwait(false);
        return label == null ? Result<LabelDto>.NotFound() : Result<LabelDto>.Success(label.Adapt<LabelDto>());
    }

    /// <inheritdoc />
    protected override bool ApplyPatch(Label entity, UpdateLabelDto patchDto)
    {
        return PartialUpdateHelper.ApplyIfChanged(entity.Name, patchDto.Name, v => entity.Name = v)
            | PartialUpdateHelper.ApplyIfChanged(entity.SlugName, patchDto.SlugName, v => entity.SlugName = v);
    }
}
