using WebShop.Api.Filters.Factories;
using WebShop.Api.Filters.Validators;

namespace WebShop.Api.Extensions.Features;

/// <summary>
/// Extension methods for configuring filter-related services.
/// </summary>
public static class FilterExtensions
{
    /// <summary>
    /// Configures filter services including ASM authorization validators and response factories.
    /// </summary>
    public static void ConfigureFilterServices(this IServiceCollection services)
    {
        // Register ASM authorization validator
        services.AddScoped<IAsmPermissionValidator, AsmPermissionValidator>();

        // Register ASM error response factory
        services.AddScoped<IAsmErrorResponseFactory, AsmErrorResponseFactory>();
    }
}
