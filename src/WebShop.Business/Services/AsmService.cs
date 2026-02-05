using Mapster;
using Microsoft.Extensions.Logging;
using WebShop.Business.DTOs;
using WebShop.Core.Helpers;
using WebShop.Core.Interfaces.Base;
using WebShop.Core.Models;
using WebShop.Util.Security;

namespace WebShop.Business.Services;

/// <summary>
/// Provides a person's application access (what they can use per role and position) so the API can enforce allowed actions.
/// Caches the ASM response until the token expires to reduce calls to the ASM service, similar to JWT token validation caching.
/// </summary>
public class AsmService(
    Core.Interfaces.Services.IAsmService asmService,
    ICacheService cacheService,
    ILogger<AsmService> logger) : Interfaces.IAsmService
{
    private readonly Core.Interfaces.Services.IAsmService _asmService = asmService ?? throw new ArgumentNullException(nameof(asmService));
    private readonly ICacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    private readonly ILogger<AsmService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    private static readonly TimeSpan DefaultCacheExpiration = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public async Task<IReadOnlyList<AsmResponseDto>> GetApplicationSecurityAsync(
        string personId,
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personId, nameof(personId));
        ArgumentException.ThrowIfNullOrWhiteSpace(token, nameof(token));

        TimeSpan? cacheExpiration = JwtTokenHelper.GetCacheExpiration(token);
        string cacheKey = CacheKeys.AsmSecurity(JwtTokenHelper.GenerateCacheKey(token));

        try
        {
            List<AsmResponseDto> result = await _cacheService.GetOrCreateAsync(
                cacheKey,
                async cancel =>
                {
                    _logger.LogDebug("ASM application security not in cache, fetching for person ID: {PersonId}", personId);
                    IReadOnlyList<AsmResponseModel> items = await _asmService.GetApplicationSecurityAsync(personId, token, cancel).ConfigureAwait(false);
                    return items.Adapt<List<AsmResponseDto>>();
                },
                expiration: cacheExpiration ?? DefaultCacheExpiration,
                localExpiration: cacheExpiration ?? DefaultCacheExpiration,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ASM application security cache lookup for person ID: {PersonId}", personId);
            // Fall back to direct call (fail-open for availability)
            IReadOnlyList<AsmResponseModel> items = await _asmService.GetApplicationSecurityAsync(personId, token, cancellationToken).ConfigureAwait(false);
            return items.Adapt<IReadOnlyList<AsmResponseDto>>();
        }
    }
}

