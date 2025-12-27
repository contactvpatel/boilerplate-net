using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebShop.Core.Interfaces.Services;
using WebShop.Core.Models;
using WebShop.Infrastructure.Helpers;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.Services.External;

/// <summary>
/// Infrastructure implementation of ASM service using HttpClient.
/// Communicates with the external ASM (Application Security Management) service.
/// </summary>
public class AsmService(
    IHttpClientFactory httpClientFactory,
    IOptions<AsmServiceOptions> options,
    ILogger<AsmService> logger,
    IOptions<HttpResilienceOptions> resilienceOptions) : HttpServiceBase(httpClientFactory, logger, resilienceOptions), IAsmService
{
    private readonly AsmServiceOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Gets the name of the HTTP client to use from the factory.
    /// </summary>
    protected override string HttpClientName => "AsmService";

    /// <inheritdoc />
    public async Task<IReadOnlyList<AsmResponseModel>> GetApplicationSecurityAsync(
        string personId,
        string token,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching application security for person ID: {PersonId}", personId);

        string endpoint = $"{_options.Endpoint.ApplicationSecurity.EnsureTrailingSlash()}?personId={personId}";

        // Use base class method with per-request Bearer token configuration (thread-safe HttpRequestMessage)
        IReadOnlyList<AsmResponseModel> securityInfo = await GetCollectionAsync<AsmResponseModel>(
            endpoint,
            request => request.SetBearerToken(token),
            cancellationToken).ConfigureAwait(false);

        _logger.LogDebug("Successfully fetched {Count} application security records for person ID: {PersonId}", securityInfo.Count, personId);
        return securityInfo;
    }

    /// <summary>
    /// Creates and configures an HttpClient for ASM service calls.
    /// Service-level headers are configured at the HttpClient factory level for thread safety.
    /// </summary>
    protected override HttpClient CreateHttpClient()
    {
        // Headers are configured at factory level in DependencyInjection for thread safety
        return _httpClientFactory.CreateClient(HttpClientName);
    }
}
