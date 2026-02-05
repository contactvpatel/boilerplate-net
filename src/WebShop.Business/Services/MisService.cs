using Mapster;
using WebShop.Business.DTOs;
using WebShop.Core.Helpers;
using WebShop.Core.Interfaces.Base;
using WebShop.Core.Models;

namespace WebShop.Business.Services;

/// <summary>
/// Business layer implementation of MIS service with caching support.
/// This service wraps the core MIS service, maps Core models to DTOs, and adds caching to reduce external API calls.
/// Uses HybridCache via ICacheService to prevent cache stampede and reduce load on external MIS system.
/// </summary>
public class MisService(
    Core.Interfaces.Services.IMisService coreMisService,
    ICacheService cacheService) : Interfaces.IMisService
{
    private readonly Core.Interfaces.Services.IMisService _coreMisService = coreMisService ?? throw new ArgumentNullException(nameof(coreMisService));
    private readonly ICacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));

    /// <inheritdoc />
    public async Task<IReadOnlyList<DepartmentDto>> GetAllDepartmentsAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrCreateAsync(
            CacheKeys.Departments(divisionId),
            async cancel =>
            {
                IReadOnlyList<DepartmentModel> models = await _coreMisService.GetAllDepartmentsAsync(divisionId, cancel).ConfigureAwait(false);
                IReadOnlyList<DepartmentDto> dtos = models.Adapt<IReadOnlyList<DepartmentDto>>();
                // Order by department name for consistent results
                return dtos.OrderBy(x => x.Name).ToList();
            },
            expiration: TimeSpan.FromHours(24), // 24 hours cache
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DepartmentDto?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        // Use cached departments to avoid additional API call
        // Default to division 1 if not found in cache (common case)
        IReadOnlyList<DepartmentDto> departments = await GetAllDepartmentsAsync(1, cancellationToken).ConfigureAwait(false);
        return departments.FirstOrDefault(x => x.Id == departmentId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleTypeDto>> GetAllRoleTypesAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrCreateAsync(
            CacheKeys.RoleTypes,
            async cancel =>
            {
                IReadOnlyList<RoleTypeModel> models = await _coreMisService.GetAllRoleTypesAsync(divisionId, cancel).ConfigureAwait(false);
                IReadOnlyList<RoleTypeDto> dtos = models.Adapt<IReadOnlyList<RoleTypeDto>>();
                // Order by role type name for consistent results
                return dtos.OrderBy(x => x.Name).ToList();
            },
            expiration: TimeSpan.FromHours(24), // 24 hours cache
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrCreateAsync(
            CacheKeys.Roles,
            async cancel =>
            {
                IReadOnlyList<RoleModel> models = await _coreMisService.GetAllRolesAsync(divisionId, cancel).ConfigureAwait(false);
                IReadOnlyList<RoleDto> dtos = models.Adapt<IReadOnlyList<RoleDto>>();
                // Order by role name for consistent results
                return dtos.OrderBy(x => x.Name).ToList();
            },
            expiration: TimeSpan.FromHours(12), // 12 hours cache (roles change more frequently)
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<RoleDto?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        // Use cached roles to avoid additional API call
        // Default to division 1 if not found in cache (common case)
        IReadOnlyList<RoleDto> roles = await GetAllRolesAsync(1, cancellationToken).ConfigureAwait(false);
        return roles.FirstOrDefault(x => x.Id == roleId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleDto>> GetRolesByDepartmentIdAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        return await _cacheService.GetOrCreateAsync(
            CacheKeys.RolesByDepartment(departmentId),
            async cancel =>
            {
                IReadOnlyList<RoleModel> models = await _coreMisService.GetRolesByDepartmentIdAsync(departmentId, cancel).ConfigureAwait(false);
                IReadOnlyList<RoleDto> dtos = models.Adapt<IReadOnlyList<RoleDto>>();
                // Order by role name for consistent results
                return dtos.OrderBy(x => x.Name).ToList();
            },
            expiration: TimeSpan.FromHours(12), // 12 hours cache
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PositionDto>> GetPositionsByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PositionModel> models = await _coreMisService.GetPositionsByRoleIdAsync(roleId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<PositionDto> dtos = models.Adapt<IReadOnlyList<PositionDto>>();
        return dtos;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonPositionDto>> GetPersonPositionsAsync(string personId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personId, nameof(personId));

        string cacheKey = CacheKeys.PersonPositions(personId);

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                IReadOnlyList<PersonPositionModel> models = await _coreMisService.GetPersonPositionsAsync(personId, cancel).ConfigureAwait(false);
                IReadOnlyList<PersonPositionDto> dtos = models.Adapt<IReadOnlyList<PersonPositionDto>>();
                // Order by position name for consistent results
                IReadOnlyList<PersonPositionDto> ordered = dtos.OrderBy(x => x.PositionName).ToList();
                return ordered;
            },
            expiration: TimeSpan.FromMinutes(20), // 20 minutes cache (person positions change more frequently)
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

