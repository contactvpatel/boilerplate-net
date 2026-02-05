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

    private const string HeaderAuthAppId = "x-baps-auth-app-id";
    private const string HeaderAuthAppSecret = "x-baps-auth-app-secret";

    /// <summary>
    /// Gets the name of the HTTP client to use from the factory.
    /// </summary>
    protected override string HttpClientName => "MisService";

    /// <summary>
    /// Configures authentication headers (AuthAppId, AuthAppSecret) on the request from options.
    /// </summary>
    private void ConfigureAuthHeaders(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(_options.Headers.AuthAppId))
        {
            request.AddHeader(HeaderAuthAppId, _options.Headers.AuthAppId);
        }
        if (!string.IsNullOrWhiteSpace(_options.Headers.AuthAppSecret))
        {
            request.AddHeader(HeaderAuthAppSecret, _options.Headers.AuthAppSecret);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<DepartmentModel>> GetAllDepartmentsAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching all departments for division ID: {DivisionId}", divisionId);

        string endpoint = $"{_options.Endpoint.Department}?divisionId={divisionId}";
        MisResponse<DepartmentModel>? response = await ExecuteAsync<DepartmentModel>(endpoint, cancellationToken).ConfigureAwait(false);

        if (response == null)
        {
            RaiseNullResponseException();
            return null!;
        }

        if (!response.Succeeded)
        {
            RaiseApplicationException(response);
        }

        _logger.LogDebug("Successfully fetched {Count} departments for division ID: {DivisionId}",
            response.Data?.Count ?? 0, divisionId);
        return response.Data ?? [];
    }

    /// <inheritdoc />
    public async Task<DepartmentModel?> GetDepartmentByIdAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching department by ID: {DepartmentId}", departmentId);

        string endpoint = $"{_options.Endpoint.Department}/{departmentId}";
        try
        {
            MisResponse<DepartmentModel>? response = await ExecuteAsync<DepartmentModel>(endpoint, cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                RaiseNullResponseException();
                return null!;
            }

            if (!response.Succeeded)
            {
                RaiseApplicationException(response);
            }

            return response.Data?.FirstOrDefault();
        }
        catch (HttpRequestException ex) when (IsNotFoundResponse(ex))
        {
            _logger.LogDebug("Department with ID {DepartmentId} not found", departmentId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleTypeModel>> GetAllRoleTypesAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching all role types for division ID: {DivisionId}", divisionId);

        string endpoint = $"{_options.Endpoint.RoleType}?divisionId={divisionId}";
        MisResponse<RoleTypeModel>? response = await ExecuteAsync<RoleTypeModel>(endpoint, cancellationToken).ConfigureAwait(false);

        if (response == null)
        {
            RaiseNullResponseException();
            return null!;
        }

        if (!response.Succeeded)
        {
            RaiseApplicationException(response);
        }

        _logger.LogDebug("Successfully fetched {Count} role types for division ID: {DivisionId}", response.Data?.Count ?? 0, divisionId);
        return response.Data ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleModel>> GetAllRolesAsync(int divisionId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching all roles for division ID: {DivisionId}", divisionId);

        string endpoint = $"{_options.Endpoint.Role}?divisionId={divisionId}";
        MisResponse<RoleModel>? response = await ExecuteAsync<RoleModel>(endpoint, cancellationToken).ConfigureAwait(false);

        if (response == null)
        {
            RaiseNullResponseException();
            return null!;
        }

        if (!response.Succeeded)
        {
            RaiseApplicationException(response);
        }

        _logger.LogDebug("Successfully fetched {Count} roles for division ID: {DivisionId}", response.Data?.Count ?? 0, divisionId);
        return response.Data ?? [];
    }

    /// <inheritdoc />
    public async Task<RoleModel?> GetRoleByIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching role by ID: {RoleId}", roleId);

        string endpoint = $"{_options.Endpoint.Role}/{roleId}";
        try
        {
            MisResponse<RoleModel>? response = await ExecuteAsync<RoleModel>(endpoint, cancellationToken).ConfigureAwait(false);

            if (response == null)
            {
                RaiseNullResponseException();
                return null!;
            }

            if (!response.Succeeded)
            {
                RaiseApplicationException(response);
            }

            return response.Data?.FirstOrDefault();
        }
        catch (HttpRequestException ex) when (IsNotFoundResponse(ex))
        {
            _logger.LogDebug("Role with ID {RoleId} not found", roleId);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RoleModel>> GetRolesByDepartmentIdAsync(int departmentId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching roles by department ID: {DepartmentId}", departmentId);

        string endpoint = $"{_options.Endpoint.Role}/departments/{departmentId}";
        MisResponse<RoleModel>? response = await ExecuteAsync<RoleModel>(endpoint, cancellationToken).ConfigureAwait(false);

        if (response == null)
        {
            RaiseNullResponseException();
            return null!;
        }

        if (!response.Succeeded)
        {
            RaiseApplicationException(response);
        }

        _logger.LogDebug("Successfully fetched {Count} roles for department ID: {DepartmentId}", response.Data?.Count ?? 0, departmentId);
        return response.Data ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PositionModel>> GetPositionsByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching positions by role ID: {RoleId}", roleId);

        string endpoint = $"{_options.Endpoint.Position}/roles/{roleId}";
        MisResponse<PositionModel>? response = await ExecuteAsync<PositionModel>(endpoint, cancellationToken).ConfigureAwait(false);

        if (response == null)
        {
            RaiseNullResponseException();
            return null!;
        }

        if (!response.Succeeded)
        {
            RaiseApplicationException(response);
        }

        _logger.LogDebug("Successfully fetched {Count} positions for role ID: {RoleId}", response.Data?.Count ?? 0, roleId);
        return response.Data ?? [];
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PersonPositionModel>> GetPersonPositionsAsync(string personId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching person positions for person ID: {PersonId}", personId);

        string endpoint = $"{_options.Endpoint.PersonPosition}?personId={personId}";
        MisResponse<PersonPositionModel>? response = await ExecuteAsync<PersonPositionModel>(endpoint, cancellationToken).ConfigureAwait(false);

        if (response == null)
        {
            RaiseNullResponseException();
            return null!;
        }

        if (!response.Succeeded)
        {
            RaiseApplicationException(response);
        }

        _logger.LogDebug("Successfully fetched {Count} positions for person ID: {PersonId}",
            response.Data?.Count ?? 0, personId);
        return response.Data ?? [];
    }

    /// <summary>
    /// Executes a GET request and deserializes the response as <see cref="MisResponse{T}"/>.
    /// Uses the same HttpClient factory and request configuration pattern as <see cref="AsmService"/>.
    /// </summary>
    private async Task<MisResponse<T>?> ExecuteAsync<T>(string endpoint, CancellationToken cancellationToken = default)
    {
        return await GetAsync<MisResponse<T>>(endpoint, ConfigureAuthHeaders, cancellationToken).ConfigureAwait(false);
    }

    private void RaiseApplicationException<T>(MisResponse<T> response)
    {
        MisErrorModel? first = response.Errors?.FirstOrDefault();
        string errorMessage = $"{first?.ErrorId}-{first?.StatusCode}-{first?.Message}";
        _logger.LogError("MIS service error: {ErrorMessage}", errorMessage);
        throw new ApplicationException(errorMessage);
    }

    private void RaiseNullResponseException()
    {
        const string errorMessage = "Received NULL response from MIS Api.";
        _logger.LogError("MIS service error: {ErrorMessage}", errorMessage);
        throw new InvalidOperationException(errorMessage);
    }

    private static bool IsNotFoundResponse(HttpRequestException ex)
    {
        return ex.StatusCode == HttpStatusCode.NotFound;
    }

    /// <summary>
    /// Creates and configures an HttpClient for MIS service calls.
    /// </summary>
    protected override HttpClient CreateHttpClient()
    {
        return _httpClientFactory.CreateClient(HttpClientName);
    }
}
