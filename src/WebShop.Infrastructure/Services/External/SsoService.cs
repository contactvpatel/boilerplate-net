using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WebShop.Core.Models;
using WebShop.Infrastructure.Helpers;
using WebShop.Util.Models;

namespace WebShop.Infrastructure.Services.External;

/// <summary>
/// Infrastructure implementation of the core SSO service using HttpClient.
/// Communicates with the external SSO service to validate tokens, renew tokens, and handle logout.
/// </summary>
public class SsoService(
    IHttpClientFactory httpClientFactory,
    IOptions<SsoServiceOptions> options,
    ILogger<SsoService> logger,
    IOptions<HttpResilienceOptions> resilienceOptions) : HttpServiceBase(httpClientFactory, logger, resilienceOptions), Core.Interfaces.Services.ISsoService
{
    private readonly SsoServiceOptions _options = options?.Value ?? throw new ArgumentNullException(nameof(options));

    /// <summary>
    /// Gets the name of the HTTP client to use from the factory.
    /// </summary>
    protected override string HttpClientName => "SsoService";

    /// <inheritdoc />
    public async Task<bool> ValidateTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("Token validation failed: token is null or empty");
            return false;
        }

        try
        {
            string endpoint = _options.Url.CombineUrl(_options.Endpoint.ValidateToken);
            ValidateTokenRequest request = new() { Token = token };
            return await PostAsync(endpoint, request, cancellationToken);
        }
        catch (HttpRequestException)
        {
            // Return false for any HTTP errors (e.g., Unauthorized, NotFound, etc.)
            _logger.LogWarning("Token validation failed: HTTP error occurred");
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<SsoAuthResponse?> RenewTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Renewing token");

        if (string.IsNullOrWhiteSpace(accessToken) || string.IsNullOrWhiteSpace(refreshToken))
        {
            _logger.LogWarning("Renew token failed: access token or refresh token is missing");
            return null;
        }

        string endpoint = _options.Url.CombineUrl(_options.Endpoint.RenewToken);
        RenewTokenRequest request = new() { RefreshToken = refreshToken };

        // Use base class method with per-request Bearer token configuration (thread-safe HttpRequestMessage)
        return await PostAsync<RenewTokenRequest, SsoAuthResponse>(
            endpoint,
            request,
            httpRequest => httpRequest.SetBearerToken(accessToken),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> LogoutAsync(string token, CancellationToken cancellationToken = default)
    {
        const string Area = "SsoService.LogoutAsync";
        _logger.LogInformation("{Area}: Logging out user", Area);

        if (string.IsNullOrWhiteSpace(token))
        {
            _logger.LogWarning("{Area}: Logout failed: token is missing", Area);
            return false;
        }

        string endpoint = _options.Url.CombineUrl(_options.Endpoint.Logout);

        // Use base class method with per-request Bearer token configuration (thread-safe HttpRequestMessage)
        return await PostAsync(
            endpoint,
            request => request.SetBearerToken(token),
            cancellationToken);
    }

    /// <summary>
    /// Creates and configures an HttpClient for SSO service calls.
    /// Base address is configured at the HttpClient factory level.
    /// </summary>
    protected override HttpClient CreateHttpClient()
    {
        // Base address is configured at factory level in DependencyInjection
        return _httpClientFactory.CreateClient(HttpClientName);
    }
}
