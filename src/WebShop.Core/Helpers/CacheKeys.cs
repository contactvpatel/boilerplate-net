namespace WebShop.Core.Helpers;

/// <summary>
/// Centralized cache key building for application-wide cache entries.
/// Use these helpers to avoid magic strings and ensure consistency.
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Prefix for ASM (application security) cache entries keyed by token.
    /// </summary>
    public const string AsmSecurityPrefix = "asm-security-";

    /// <summary>
    /// Prefix for person-positions cache entries (MIS person positions).
    /// Format: person-{personId}-positions
    /// </summary>
    public const string PersonPositionsPrefix = "person-";

    /// <summary>
    /// Suffix for person-positions cache key.
    /// </summary>
    public const string PersonPositionsSuffix = "-positions";

    /// <summary>
    /// Builds the cache key for ASM application security data for a given token cache key.
    /// </summary>
    /// <param name="tokenCacheKey">The token cache key (e.g. from JwtTokenHelper.GenerateCacheKey).</param>
    /// <returns>The full cache key for ASM security data.</returns>
    public static string AsmSecurity(string tokenCacheKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tokenCacheKey, nameof(tokenCacheKey));
        return $"{AsmSecurityPrefix}{tokenCacheKey}";
    }

    /// <summary>
    /// Builds the cache key for a person's positions (MIS data).
    /// </summary>
    /// <param name="personId">The person/user identifier.</param>
    /// <returns>The full cache key for person positions.</returns>
    public static string PersonPositions(string personId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personId, nameof(personId));
        return $"{PersonPositionsPrefix}{personId}{PersonPositionsSuffix}";
    }

    /// <summary>
    /// Builds the cache key for departments by division (MIS data).
    /// </summary>
    /// <param name="divisionId">The division identifier.</param>
    /// <returns>The full cache key for departments.</returns>
    public static string Departments(int divisionId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(divisionId, 1, nameof(divisionId));
        return $"departments-division-{divisionId}";
    }

    /// <summary>
    /// Cache key for role types (MIS data). Shared across divisions in current implementation.
    /// </summary>
    public const string RoleTypes = "role-types";

    /// <summary>
    /// Cache key for roles by division (MIS data). Shared across divisions in current implementation.
    /// </summary>
    public const string Roles = "roles";

    /// <summary>
    /// Builds the cache key for roles by department (MIS data).
    /// </summary>
    /// <param name="departmentId">The department identifier.</param>
    /// <returns>The full cache key for roles by department.</returns>
    public static string RolesByDepartment(int departmentId)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(departmentId, 1, nameof(departmentId));
        return $"roles-department-{departmentId}";
    }
}
