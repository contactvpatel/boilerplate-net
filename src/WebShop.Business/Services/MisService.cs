using Mapster;
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
    public async Task<IReadOnlyList<DTOs.DepartmentDto>> GetAllDepartmentsAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"departments-division-{divisionId}";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                IReadOnlyList<DepartmentModel> models = await _coreMisService.GetAllDepartmentsAsync(divisionId, cancel).ConfigureAwait(false);
                IReadOnlyList<DTOs.DepartmentDto> dtos = models.Adapt<IReadOnlyList<DTOs.DepartmentDto>>();
                // Order by department name for consistent results
                return dtos.OrderBy(x => x.Name).ToList();
            },
            expiration: TimeSpan.FromHours(24), // 24 hours cache
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DTOs.DepartmentDto?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        // Use cached departments to avoid additional API call
        // Default to division 1 if not found in cache (common case)
        IReadOnlyList<DTOs.DepartmentDto> departments = await GetAllDepartmentsAsync(1, cancellationToken).ConfigureAwait(false);
        return departments.FirstOrDefault(x => x.Id == departmentId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DTOs.RoleTypeDto>> GetAllRoleTypesAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        const string CacheKey = "role-types";

        return await _cacheService.GetOrCreateAsync(
            CacheKey,
            async cancel =>
            {
                IReadOnlyList<RoleTypeModel> models = await _coreMisService.GetAllRoleTypesAsync(divisionId, cancel).ConfigureAwait(false);
                IReadOnlyList<DTOs.RoleTypeDto> dtos = models.Adapt<IReadOnlyList<DTOs.RoleTypeDto>>();
                // Order by role type name for consistent results
                return dtos.OrderBy(x => x.Name).ToList();
            },
            expiration: TimeSpan.FromHours(24), // 24 hours cache
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DTOs.RoleDto>> GetAllRolesAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        const string CacheKey = "roles";

        return await _cacheService.GetOrCreateAsync(
            CacheKey,
            async cancel =>
            {
                IReadOnlyList<RoleModel> models = await _coreMisService.GetAllRolesAsync(divisionId, cancel).ConfigureAwait(false);
                IReadOnlyList<DTOs.RoleDto> dtos = models.Adapt<IReadOnlyList<DTOs.RoleDto>>();
                // Order by role name for consistent results
                return dtos.OrderBy(x => x.Name).ToList();
            },
            expiration: TimeSpan.FromHours(12), // 12 hours cache (roles change more frequently)
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<DTOs.RoleDto?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        // Use cached roles to avoid additional API call
        // Default to division 1 if not found in cache (common case)
        IReadOnlyList<DTOs.RoleDto> roles = await GetAllRolesAsync(1, cancellationToken).ConfigureAwait(false);
        return roles.FirstOrDefault(x => x.Id == roleId);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DTOs.RoleDto>> GetRolesByDepartmentIdAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"roles-department-{departmentId}";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                IReadOnlyList<RoleModel> models = await _coreMisService.GetRolesByDepartmentIdAsync(departmentId, cancel).ConfigureAwait(false);
                IReadOnlyList<DTOs.RoleDto> dtos = models.Adapt<IReadOnlyList<DTOs.RoleDto>>();
                // Order by role name for consistent results
                return dtos.OrderBy(x => x.Name).ToList();
            },
            expiration: TimeSpan.FromHours(12), // 12 hours cache
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DTOs.PositionDto>> GetPositionsByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<PositionModel> models = await _coreMisService.GetPositionsByRoleIdAsync(roleId, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<DTOs.PositionDto> dtos = models.Adapt<IReadOnlyList<DTOs.PositionDto>>();
        return dtos;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DTOs.PersonPositionDto>> GetPersonPositionsAsync(string personId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personId, nameof(personId));

        string cacheKey = $"person-{personId}-positions";

        return await _cacheService.GetOrCreateAsync(
            cacheKey,
            async cancel =>
            {
                IReadOnlyList<PersonPositionModel> models = await _coreMisService.GetPersonPositionsAsync(personId, cancel).ConfigureAwait(false);
                IReadOnlyList<DTOs.PersonPositionDto> dtos = models.Adapt<IReadOnlyList<DTOs.PersonPositionDto>>();
                // Order by position name for consistent results
                IReadOnlyList<DTOs.PersonPositionDto> ordered = dtos.OrderBy(x => x.PositionName).ToList();
                return ordered;
            },
            expiration: TimeSpan.FromMinutes(20), // 20 minutes cache (person positions change more frequently)
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}

