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
    /// <summary>
    /// HTTP client name used to call the ASM service.
    /// </summary>
    protected override string HttpClientName => "AsmService";

    /// <summary>
    /// Adds the application credentials required by ASM to accept the request.
    /// </summary>
    private void ConfigureAuthHeaders(HttpRequestMessage request)
    {
        if (!string.IsNullOrWhiteSpace(options.Value.Headers.AuthAppId))
        {
            request.AddHeader(HttpHeaderNames.AsmAuthAppId, options.Value.Headers.AuthAppId);
        }
        if (!string.IsNullOrWhiteSpace(options.Value.Headers.AuthAppSecret))
        {
            request.AddHeader(HttpHeaderNames.AsmAuthAppSecret, options.Value.Headers.AuthAppSecret);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AsmResponseModel>> GetApplicationSecurityAsync(
        string personId,
        string token,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching application security for Person ID: {PersonId}", personId);

        string endpoint = options.Value.Endpoint.ApplicationSecurity.EnsureTrailingSlash();
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
