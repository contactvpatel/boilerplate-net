using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebShop.Core.Interfaces.Services;
using WebShop.Core.Models;
using WebShop.Infrastructure.Helpers;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.Services.External;

/// <summary>
/// Infrastructure implementation of MIS service using HttpClient.
/// Communicates with the external MIS (Management Information System) service.
/// </summary>
public class MisService(
    IHttpClientFactory httpClientFactory,
    IOptions<MisServiceOptions> options,
    ILogger<MisService> logger,
    IOptions<HttpResilienceOptions> resilienceOptions) : HttpServiceBase(httpClientFactory, logger, resilienceOptions), IMisService
{
    private readonly MisServiceOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Gets the name of the HTTP client to use from the factory.
    /// </summary>
    protected override string HttpClientName => "MisService";

    /// <inheritdoc />
    public async Task<IReadOnlyList<DepartmentModel>> GetAllDepartmentsAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching all departments for division ID: {DivisionId}", divisionId);

        string endpoint = $"{_options.Endpoint.Department}?divisionId={divisionId}";
        IReadOnlyList<DepartmentModel> departments = await GetCollectionAsync<DepartmentModel>(endpoint, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Successfully fetched {Count} departments for division ID: {DivisionId}",
            departments.Count, divisionId);
        return departments;
    }

    /// <inheritdoc />
    public async Task<DepartmentModel?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching department by ID: {DepartmentId}", departmentId);

        string endpoint = $"{_options.Endpoint.Department}/{departmentId}";
        try
        {
            return await GetAsync<DepartmentModel>(endpoint, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Not found", StringComparison.OrdinalIgnoreCase))
        {
            // Return null for not found instead of throwing
            _logger.LogDebug("Department with ID {DepartmentId} not found", departmentId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleTypeModel>> GetAllRoleTypesAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching all role types for division ID: {DivisionId}", divisionId);

        string endpoint = $"{_options.Endpoint.RoleType}?divisionId={divisionId}";
        IReadOnlyList<RoleTypeModel> roleTypes = await GetCollectionAsync<RoleTypeModel>(endpoint, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Successfully fetched {Count} role types for division ID: {DivisionId}", roleTypes.Count, divisionId);
        return roleTypes;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleModel>> GetAllRolesAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching all roles for division ID: {DivisionId}", divisionId);

        string endpoint = $"{_options.Endpoint.Role}?divisionId={divisionId}";
        IReadOnlyList<RoleModel> roles = await GetCollectionAsync<RoleModel>(endpoint, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Successfully fetched {Count} roles for division ID: {DivisionId}", roles.Count, divisionId);
        return roles;
    }

    /// <inheritdoc />
    public async Task<RoleModel?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching role by ID: {RoleId}", roleId);

        string endpoint = $"{_options.Endpoint.Role}/{roleId}";
        return await GetAsync<RoleModel>(endpoint, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleModel>> GetRolesByDepartmentIdAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching roles by department ID: {DepartmentId}", departmentId);

        string endpoint = $"{_options.Endpoint.Role}/departments/{departmentId}";
        IReadOnlyList<RoleModel> roles = await GetCollectionAsync<RoleModel>(endpoint, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Successfully fetched {Count} roles for department ID: {DepartmentId}", roles.Count, departmentId);
        return roles;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PositionModel>> GetPositionsByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching positions by role ID: {RoleId}", roleId);

        string endpoint = $"{_options.Endpoint.Position}/roles/{roleId}";
        IReadOnlyList<PositionModel> positions = await GetCollectionAsync<PositionModel>(endpoint, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Successfully fetched {Count} positions for role ID: {RoleId}", positions.Count, roleId);
        return positions;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonPositionModel>> GetPersonPositionsAsync(string personId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching person positions for person ID: {PersonId}", personId);

        string endpoint = $"{_options.Endpoint.PersonPosition}?personId={personId}";
        IReadOnlyList<PersonPositionModel> positions = await GetCollectionAsync<PersonPositionModel>(endpoint, cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Successfully fetched {Count} positions for person ID: {PersonId}",
            positions.Count, personId);
        return positions;
    }

    /// <summary>
    /// Creates and configures an HttpClient for MIS service calls.
    /// Service-level headers are configured at the HttpClient factory level for thread safety.
    /// </summary>
    protected override HttpClient CreateHttpClient()
    {
        // Headers are configured at factory level in DependencyInjection for thread safety
        return _httpClientFactory.CreateClient(HttpClientName);
    }
}
