using WebShop.Core.Models;

namespace WebShop.Core.Interfaces.Services;

/// <summary>
/// Service interface for Management Information System (MIS) operations.
/// </summary>
public interface IMisService
{
    /// <summary>
    /// Gets all departments for a division.
    /// </summary>
    /// <param name="divisionId">Division identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of departments.</returns>
    Task<IReadOnlyList<DepartmentModel>> GetAllDepartmentsAsync(int divisionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a department by identifier.
    /// </summary>
    /// <param name="departmentId">Department identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Department if found, null otherwise.</returns>
    Task<DepartmentModel?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all role types for a division.
    /// </summary>
    /// <param name="divisionId">Division identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of role types.</returns>
    Task<IReadOnlyList<RoleTypeModel>> GetAllRoleTypesAsync(int divisionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles for a division.
    /// </summary>
    /// <param name="divisionId">Division identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of roles.</returns>
    Task<IReadOnlyList<RoleModel>> GetAllRolesAsync(int divisionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a role by identifier.
    /// </summary>
    /// <param name="roleId">Role identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Role if found, null otherwise.</returns>
    Task<RoleModel?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all roles for a department.
    /// </summary>
    /// <param name="departmentId">Department identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of roles.</returns>
    Task<IReadOnlyList<RoleModel>> GetRolesByDepartmentIdAsync(int departmentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all positions for a role.
    /// </summary>
    /// <param name="roleId">Role identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of positions.</returns>
    Task<IReadOnlyList<PositionModel>> GetPositionsByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all positions for a person.
    /// </summary>
    /// <param name="personId">Person identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of person positions.</returns>
    Task<IReadOnlyList<PersonPositionModel>> GetPersonPositionsAsync(string personId, CancellationToken cancellationToken = default);
}
