using Mapster;
using WebShop.Core.Models;

namespace WebShop.Business.Services;

/// <summary>
/// Business layer implementation of ASM service.
/// This service wraps the core ASM service and maps Core models to DTOs.
/// </summary>
public class AsmService(Core.Interfaces.Services.IAsmService coreAsmService) : Interfaces.IAsmService
{
    private readonly Core.Interfaces.Services.IAsmService _coreAsmService = coreAsmService ?? throw new ArgumentNullException(nameof(coreAsmService));

    /// <inheritdoc />
    public async Task<IReadOnlyList<DTOs.AsmResponseDto>> GetApplicationSecurityAsync(
        string personId,
        string token,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(personId, nameof(personId));
        ArgumentException.ThrowIfNullOrWhiteSpace(token, nameof(token));

        IReadOnlyList<AsmResponseModel> models = await _coreAsmService.GetApplicationSecurityAsync(personId, token, cancellationToken).ConfigureAwait(false);
        IReadOnlyList<DTOs.AsmResponseDto> dtos = models.Adapt<IReadOnlyList<DTOs.AsmResponseDto>>();
        return dtos;
    }
}

