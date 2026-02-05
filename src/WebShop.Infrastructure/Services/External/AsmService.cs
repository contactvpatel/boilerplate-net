using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebShop.Core.Interfaces.Services;
using WebShop.Core.Models;
using WebShop.Infrastructure.Helpers;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.Services.External;

/// <summary>
/// Loads a user's application security (what they can access) from the central Application Security Management (ASM) system.
/// </summary>
public class AsmService(
    IHttpClientFactory httpClientFactory,
    IOptions<AsmServiceOptions> options,
    ILogger<AsmService> logger,
    IOptions<HttpResilienceOptions> resilienceOptions) : HttpServiceBase(httpClientFactory, logger, resilienceOptions), IAsmService
{
    private readonly AsmServiceOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    private const string HeaderAuthAppId = "x-app-auth-id";
    private const string HeaderAuthAppSecret = "x-app-auth-secret";

    /// <summary>
    /// HTTP client name used to call the ASM service.
    /// </summary>
    protected override string HttpClientName => "AsmService";

    /// <summary>
    /// Adds the application credentials required by ASM to accept the request.
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
    public async Task<IReadOnlyList<AsmResponseModel>> GetApplicationSecurityAsync(
        string personId,
        string token,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching application security for Person ID: {PersonId}", personId);

        string endpoint = _options.Endpoint.ApplicationSecurity.EnsureTrailingSlash();
        AsmApiResponse? response = await GetAsync<AsmApiResponse>(
            endpoint,
            request =>
            {
                ConfigureAuthHeaders(request);
                request.SetBearerToken(token);
            },
            cancellationToken).ConfigureAwait(false);

        int count = response?.Data?.Count ?? 0;

        _logger.LogDebug("Successfully fetched {Count} application security records for Person ID: {PersonId}", count, personId);

        return response?.Data ?? [];
    }

    /// <summary>
    /// Provides the HTTP client used to call the ASM service.
    /// </summary>
    protected override HttpClient CreateHttpClient()
    {
        return _httpClientFactory.CreateClient(HttpClientName);
    }
}
