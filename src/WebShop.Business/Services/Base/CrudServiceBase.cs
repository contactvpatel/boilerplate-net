using System.Linq;
using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.Helpers;
using WebShop.Business.Models;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces.Base;

namespace WebShop.Business.Services.Base;

/// <summary>
/// Generic base class for CRUD services that follow the standard pattern.
/// Reduces duplication across CustomerService, ColorService, OrderService, etc.
/// Subclasses override <see cref="ApplyPatch"/> for PATCH-specific logic and add entity-specific methods (e.g., GetByNameAsync).
/// </summary>
/// <typeparam name="TEntity">The entity type (must inherit from BaseEntity).</typeparam>
/// <typeparam name="TDto">The DTO type returned by the service.</typeparam>
/// <typeparam name="TCreateDto">The DTO type for create operations.</typeparam>
/// <typeparam name="TUpdateDto">The DTO type for update and patch operations.</typeparam>
public abstract class CrudServiceBase<TEntity, TDto, TCreateDto, TUpdateDto>(
    IRepository<TEntity> repository,
    IUnitOfWork unitOfWork,
    ILogger logger)
    where TEntity : BaseEntity
{
    /// <summary>
    /// The repository for data access.
    /// </summary>
    protected IRepository<TEntity> Repository { get; } = repository;

    /// <summary>
    /// The unit of work for transaction management.
    /// </summary>
    protected IUnitOfWork UnitOfWork { get; } = unitOfWork;

    /// <summary>
    /// Logger for the service.
    /// </summary>
    protected ILogger Logger { get; } = logger;

    /// <summary>
    /// Entity name for logging (e.g., "Color", "Customer").
    /// </summary>
    protected abstract string EntityName { get; }

    /// <summary>
    /// Plural entity name for batch logging (e.g., "Colors", "Customers").
    /// </summary>
    protected virtual string EntityNamePlural => EntityName + "s";

    /// <summary>
    /// Gets an entity by ID.
    /// </summary>
    public virtual async Task<Result<TDto>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        TEntity? entity = await Repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        return entity == null ? Result<TDto>.NotFound() : Result<TDto>.Success(entity.Adapt<TDto>());
    }

    /// <summary>
    /// Gets all entities.
    /// </summary>
    public virtual async Task<IReadOnlyList<TDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<TEntity> entities = await Repository.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return entities.Adapt<IReadOnlyList<TDto>>();
    }

    /// <summary>
    /// Gets a paginated list of entities.
    /// </summary>
    public virtual async Task<(IReadOnlyList<TDto> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        (IReadOnlyList<TEntity> items, int totalCount) = await Repository
            .GetPagedAsync(pageNumber, pageSize, cancellationToken)
            .ConfigureAwait(false);

        IReadOnlyList<TDto> dtos = items.Adapt<IReadOnlyList<TDto>>();
        return (dtos, totalCount);
    }

    /// <summary>
    /// Creates a new entity.
    /// </summary>
    public virtual async Task<TDto> CreateAsync(TCreateDto createDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDto);

        TEntity entity = createDto.Adapt<TEntity>();
        await Repository.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        await Repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Logger.LogInformation("{EntityName} created successfully. {EntityName}Id: {Id}", EntityName, EntityName, entity.Id);
        return entity.Adapt<TDto>();
    }

    /// <summary>
    /// Updates an existing entity (full update).
    /// </summary>
    public virtual async Task<Result<TDto>> UpdateAsync(int id, TUpdateDto updateDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updateDto);

        TEntity? entity = await Repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null)
        {
            return Result<TDto>.NotFound();
        }

        updateDto.Adapt(entity);
        await Repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
        await Repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        Logger.LogInformation("{EntityName} updated successfully. {EntityName}Id: {Id}", EntityName, EntityName, id);
        return Result<TDto>.Success(entity.Adapt<TDto>());
    }

    /// <summary>
    /// Partially updates an entity (PATCH). Override <see cref="ApplyPatch"/> to define entity-specific partial update logic.
    /// </summary>
    public virtual async Task<Result<TDto>> PatchAsync(int id, TUpdateDto patchDto, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(patchDto);

        TEntity? entity = await Repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null)
        {
            return Result<TDto>.NotFound();
        }

        bool hasChanges = ApplyPatch(entity, patchDto);
        if (hasChanges)
        {
            await Repository.UpdateAsync(entity, cancellationToken).ConfigureAwait(false);
            await Repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            Logger.LogInformation("{EntityName} patched successfully. {EntityName}Id: {Id}", EntityName, EntityName, id);
        }

        return Result<TDto>.Success(entity.Adapt<TDto>());
    }

    /// <summary>
    /// Applies partial update from patch DTO to entity. Override in subclasses for entity-specific fields.
    /// </summary>
    /// <param name="entity">The entity to update.</param>
    /// <param name="patchDto">The patch DTO with optional values.</param>
    /// <returns>True if any changes were applied.</returns>
    protected abstract bool ApplyPatch(TEntity entity, TUpdateDto patchDto);

    /// <summary>
    /// Soft deletes an entity. Returns true if deleted or already deleted (idempotent), false if not found.
    /// </summary>
    public virtual async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        bool exists = await Repository.ExistsAsync(id, includeSoftDeleted: true, cancellationToken).ConfigureAwait(false);
        if (!exists)
        {
            return false;
        }

        TEntity? entity = await Repository.GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
        if (entity == null)
        {
            Logger.LogInformation("{EntityName} already deleted. {EntityName}Id: {Id}", EntityName, EntityName, id);
            return true;
        }

        await Repository.DeleteAsync(entity, cancellationToken).ConfigureAwait(false);
        await Repository.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("{EntityName} deleted successfully. {EntityName}Id: {Id}", EntityName, EntityName, id);
        return true;
    }

    /// <summary>
    /// Creates multiple entities in a batch operation.
    /// </summary>
    public virtual async Task<IReadOnlyList<TDto>> CreateBatchAsync(
        IReadOnlyList<TCreateDto> createDtos,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(createDtos);

        if (createDtos.Count == 0)
        {
            return Array.Empty<TDto>();
        }

        Logger.LogInformation("Creating {Count} {EntityNamePlural} in batch", createDtos.Count, EntityNamePlural);

        List<TEntity> entities = createDtos.Select(dto => dto.Adapt<TEntity>()).ToList();

        await BatchOperationHelper.ExecuteBatchAsync(
            UnitOfWork,
            entities,
            async (entity, ct) => await Repository.AddAsync(entity, ct).ConfigureAwait(false),
            cancellationToken).ConfigureAwait(false);

        Logger.LogInformation("Batch created {Count} {EntityNamePlural} successfully", entities.Count, EntityNamePlural);
        return entities.Adapt<IReadOnlyList<TDto>>();
    }

    /// <summary>
    /// Updates multiple entities in a batch operation.
    /// </summary>
    public virtual async Task<IReadOnlyList<TDto>> UpdateBatchAsync(
        IReadOnlyList<(int Id, TUpdateDto UpdateDto)> updates,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(updates);

        if (updates.Count == 0)
        {
            return Array.Empty<TDto>();
        }

        Logger.LogInformation("Updating {Count} {EntityNamePlural} in batch", updates.Count, EntityNamePlural);

        IReadOnlyList<int> ids = updates.Select(u => u.Id).ToList();
        IReadOnlyList<TEntity> entities = await Repository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false)
            ?? Array.Empty<TEntity>();
        Dictionary<int, TEntity> entityLookup = entities.ToDictionary(e => e.Id);

        List<(int Id, TUpdateDto UpdateDto)> validUpdates = updates.Where(u => entityLookup.ContainsKey(u.Id)).ToList();
        List<TDto> updatedDtos = new(validUpdates.Count);

        await BatchOperationHelper.ExecuteBatchAsync(
            UnitOfWork,
            validUpdates,
            async (update, ct) =>
            {
                TEntity entity = entityLookup[update.Id];
                update.UpdateDto.Adapt(entity);
                await Repository.UpdateAsync(entity, ct).ConfigureAwait(false);
                updatedDtos.Add(entity.Adapt<TDto>());
            },
            cancellationToken).ConfigureAwait(false);

        Logger.LogInformation("Batch updated {Count} {EntityNamePlural} successfully", updatedDtos.Count, EntityNamePlural);
        return updatedDtos;
    }

    /// <summary>
    /// Deletes multiple entities in a batch operation (soft delete).
    /// </summary>
    public virtual async Task<IReadOnlyList<int>> DeleteBatchAsync(
        IReadOnlyList<int> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return Array.Empty<int>();
        }

        Logger.LogInformation("Deleting {Count} {EntityNamePlural} in batch", ids.Count, EntityNamePlural);

        IReadOnlyList<TEntity> entities = await Repository.FindByIdsAsync(ids, cancellationToken).ConfigureAwait(false)
            ?? Array.Empty<TEntity>();
        List<int> deletedIds = new(entities.Count);

        await BatchOperationHelper.ExecuteBatchAsync(
            UnitOfWork,
            entities,
            async (entity, ct) =>
            {
                await Repository.DeleteAsync(entity, ct).ConfigureAwait(false);
                deletedIds.Add(entity.Id);
            },
            cancellationToken).ConfigureAwait(false);

        Logger.LogInformation("Batch deleted {Count} {EntityNamePlural} successfully", deletedIds.Count, EntityNamePlural);
        return deletedIds;
    }
}
