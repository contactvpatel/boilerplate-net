using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Core.Interfaces.Base;
using IMisService = WebShop.Business.Services.Interfaces.IMisService;

namespace WebShop.Api.Controllers;

/// <summary>
/// MIS Controller for Management Information System operations.
/// Provides endpoints for departments, roles, role types, and positions.
/// </summary>
[ApiController]
[ApiVersion("1")]
[Route("api/v{version:apiVersion}/mis")]
[Produces("application/json")]
public class MisController(
    IMisService misService,
    IUserContext userContext,
    ILogger<MisController> logger) : BaseApiController
{
    private readonly IMisService _misService = misService ?? throw new ArgumentNullException(nameof(misService));
    private readonly IUserContext _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    private readonly ILogger<MisController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    /// <summary>
    /// Gets all departments for a division.
    /// </summary>
    /// <param name="divisionId">The division identifier. Defaults to 1 if not provided or set to 0.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A list of departments for the specified division, or 404 if no departments are found.</returns>
    /// <remarks>
    /// This endpoint retrieves all departments associated with a specific division from the Management Information System (MIS).
    /// If no division ID is provided or it's set to 0, the default division ID of 1 is used.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/mis/departments?divisionId=1
    /// GET /api/v1/mis/departments (defaults to divisionId=1)
    /// </code>
    /// </example>
    [HttpGet("departments")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<DepartmentDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<DepartmentDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<IReadOnlyList<DepartmentDto>>>> GetAllDepartments(
        [FromQuery] int divisionId = 0,
        CancellationToken cancellationToken = default)
    {
        divisionId = divisionId == 0 ? 1 : divisionId;

        IReadOnlyList<DepartmentDto> departments = await _misService.GetAllDepartmentsAsync(divisionId, cancellationToken);

        if (departments.Count == 0)
        {
            _logger.LogWarning("No departments found for division ID: {DivisionId}", divisionId);
            return HandleNotFound<IReadOnlyList<DepartmentDto>>("Department", "DivisionID", divisionId);
        }

        string message = $"Found {departments.Count} departments for division ID: {divisionId}";
        return Ok(Response<IReadOnlyList<DepartmentDto>>.Success(departments, message));
    }

    /// <summary>
    /// Gets a department by identifier.
    /// </summary>
    /// <param name="departmentId">Department identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Department if found, otherwise 404.</returns>
    [HttpGet("departments/{departmentId:int}")]
    [ProducesResponseType(typeof(Response<DepartmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<DepartmentDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<DepartmentDto>>> GetDepartmentById(
        [FromRoute] int departmentId,
        CancellationToken cancellationToken)
    {
        DepartmentDto? department = await _misService.GetDepartmentByIdAsync(departmentId, cancellationToken);

        if (department == null)
        {
            _logger.LogWarning("No department found with ID: {DepartmentId}", departmentId);
            return HandleNotFound<DepartmentDto>("Department", "ID", departmentId);
        }

        return Ok(Response<DepartmentDto>.Success(department, "Department retrieved successfully"));
    }

    /// <summary>
    /// Gets all role types for a division.
    /// </summary>
    /// <param name="divisionId">The division identifier. Defaults to 1 if not provided or set to 0.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A list of role types for the specified division, or 404 if no role types are found.</returns>
    /// <remarks>
    /// This endpoint retrieves all role types (categories of roles) associated with a specific division from the Management Information System (MIS).
    /// Role types define the classification of roles within an organization. If no division ID is provided or it's set to 0, the default division ID of 1 is used.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/mis/roletypes?divisionId=1
    /// GET /api/v1/mis/roletypes (defaults to divisionId=1)
    /// </code>
    /// </example>
    [HttpGet("roletypes")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<RoleTypeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<RoleTypeDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<IReadOnlyList<RoleTypeDto>>>> GetAllRoleTypes(
        [FromQuery] int divisionId = 0,
        CancellationToken cancellationToken = default)
    {
        divisionId = divisionId == 0 ? 1 : divisionId;

        IReadOnlyList<RoleTypeDto> roleTypes = await _misService.GetAllRoleTypesAsync(divisionId, cancellationToken);

        if (roleTypes.Count == 0)
        {
            _logger.LogWarning("No role types found for division ID: {DivisionId}", divisionId);
            return HandleNotFound<IReadOnlyList<RoleTypeDto>>("RoleType", "DivisionID", divisionId);
        }

        string message = $"Found {roleTypes.Count} role types for division ID: {divisionId}";
        return Ok(Response<IReadOnlyList<RoleTypeDto>>.Success(roleTypes, message));
    }

    /// <summary>
    /// Gets all roles for a division.
    /// </summary>
    /// <param name="divisionId">Division identifier. Defaults to 1 if not provided or 0.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of roles.</returns>
    [HttpGet("roles")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<RoleDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<IReadOnlyList<RoleDto>>>> GetAllRoles(
        [FromQuery] int divisionId = 0,
        CancellationToken cancellationToken = default)
    {
        divisionId = divisionId == 0 ? 1 : divisionId;

        IReadOnlyList<RoleDto> roles = await _misService.GetAllRolesAsync(divisionId, cancellationToken);

        if (roles.Count == 0)
        {
            _logger.LogWarning("No roles found for division ID: {DivisionId}", divisionId);
            return HandleNotFound<IReadOnlyList<RoleDto>>("Role", "DivisionID", divisionId);
        }

        string message = $"Found {roles.Count} roles for division ID: {divisionId}";
        return Ok(Response<IReadOnlyList<RoleDto>>.Success(roles, message));
    }

    /// <summary>
    /// Gets a role by identifier.
    /// </summary>
    /// <param name="id">Role identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Role if found, otherwise 404.</returns>
    [HttpGet("roles/{id:int}")]
    [ProducesResponseType(typeof(Response<RoleDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<RoleDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<RoleDto>>> GetRoleById(
        [FromRoute] int id,
        CancellationToken cancellationToken)
    {
        RoleDto? role = await _misService.GetRoleByIdAsync(id, cancellationToken);

        if (role == null)
        {
            _logger.LogWarning("No role found with ID: {RoleId}", id);
            return HandleNotFound<RoleDto>("Role", "ID", id);
        }

        return Ok(Response<RoleDto>.Success(role, "Role retrieved successfully"));
    }

    /// <summary>
    /// Gets all roles for a department.
    /// </summary>
    /// <param name="departmentId">Department identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of roles.</returns>
    [HttpGet("roles/departments/{departmentId:int}")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<RoleDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<RoleDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<IReadOnlyList<RoleDto>>>> GetRolesByDepartmentId(
        [FromRoute] int departmentId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<RoleDto> roles = await _misService.GetRolesByDepartmentIdAsync(departmentId, cancellationToken);

        if (roles.Count == 0)
        {
            _logger.LogWarning("No roles found for department ID: {DepartmentId}", departmentId);
            return HandleNotFound<IReadOnlyList<RoleDto>>("Role", "DepartmentID", departmentId);
        }

        string message = $"Found {roles.Count} roles for department ID: {departmentId}";
        return Ok(Response<IReadOnlyList<RoleDto>>.Success(roles, message));
    }

    /// <summary>
    /// Gets all positions for a role.
    /// </summary>
    /// <param name="roleId">Role identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of positions.</returns>
    [HttpGet("positions/roles/{roleId:int}")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<PositionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<PositionDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<IReadOnlyList<PositionDto>>>> GetPositionsByRoleId(
        [FromRoute] int roleId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<PositionDto> positions = await _misService.GetPositionsByRoleIdAsync(roleId, cancellationToken);

        if (positions.Count == 0)
        {
            _logger.LogWarning("No positions found for role ID: {RoleId}", roleId);
            return HandleNotFound<IReadOnlyList<PositionDto>>("Position", "RoleID", roleId);
        }

        string message = $"Found {positions.Count} positions for role ID: {roleId}";
        return Ok(Response<IReadOnlyList<PositionDto>>.Success(positions, message));
    }

    /// <summary>
    /// Gets all positions for the current authenticated user.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A list of positions associated with the current authenticated user, or 404 if no positions are found.</returns>
    /// <remarks>
    /// This endpoint retrieves all positions (roles and responsibilities) for the currently authenticated user from the Management Information System (MIS).
    /// The user ID is extracted from the JWT token in the Authorization header. This endpoint requires authentication.
    /// Returns 401 Unauthorized if the user ID is not available in the context.
    /// </remarks>
    /// <example>
    /// <code>
    /// GET /api/v1/mis/person-positions
    /// Authorization: Bearer {access_token}
    /// </code>
    /// </example>
    [HttpGet("person-positions")]
    [ProducesResponseType(typeof(Response<IReadOnlyList<PersonPositionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Response<IReadOnlyList<PersonPositionDto>>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Response<IReadOnlyList<PersonPositionDto>>>> GetPersonPositions(
        CancellationToken cancellationToken)
    {
        string? userId = _userContext.GetUserId();

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("User ID not available in context");
            return UnauthorizedResponse<IReadOnlyList<PersonPositionDto>>("User ID not available", "User ID not available");
        }

        IReadOnlyList<PersonPositionDto> positions = await _misService.GetPersonPositionsAsync(userId, cancellationToken);

        if (positions.Count == 0)
        {
            _logger.LogWarning("No positions found for person ID: {UserId}", userId);
            return HandleNotFound<IReadOnlyList<PersonPositionDto>>("PersonPosition", "UserID", userId);
        }

        return Ok(Response<IReadOnlyList<PersonPositionDto>>.Success(positions, "Person positions retrieved successfully"));
    }
}

