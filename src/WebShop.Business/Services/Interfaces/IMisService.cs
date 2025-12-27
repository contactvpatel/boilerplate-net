using WebShop.Business.DTOs;

namespace WebShop.Business.Services.Interfaces;

/// <summary>
/// Business layer interface for Management Information System (MIS) operations.
/// Provides access to organizational structure data (departments, roles, positions).
/// </summary>
public interface IMisService
{
    /// <summary>
    /// Gets all departments for a specific division.
    /// </summary>
    Task<IReadOnlyList<DepartmentDto>> GetAllDepartmentsAsync(int divisionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a department by its unique identifier.
    /// </summary>
    Task<DepartmentDto?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all role types (role categories) for a division.
    /// </summary>
    Task<IReadOnlyList<RoleTypeDto>> GetAllRoleTypesAsync(int divisionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles for a specific division.
    /// </summary>
    Task<IReadOnlyList<RoleDto>> GetAllRolesAsync(int divisionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by its unique identifier.
    /// </summary>
    Task<RoleDto?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles associated with a specific department.
    /// </summary>
    Task<IReadOnlyList<RoleDto>> GetRolesByDepartmentIdAsync(int departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all positions (job positions) for a specific role.
    /// </summary>
    Task<IReadOnlyList<PositionDto>> GetPositionsByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all positions held by a specific person (employee).
    /// </summary>
    Task<IReadOnlyList<PersonPositionDto>> GetPersonPositionsAsync(string personId, CancellationToken cancellationToken = default);
}

