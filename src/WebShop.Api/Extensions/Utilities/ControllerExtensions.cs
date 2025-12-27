using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using WebShop.Api.Filters;
using WebShop.Business.DTOs;

namespace WebShop.Api.Extensions.Utilities;

/// <summary>
/// Extension methods for configuring controllers.
/// </summary>
public static class ControllerExtensions
{
    /// <summary>
    /// Configures controllers with global filters and FluentValidation.
    /// </summary>
    public static void ConfigureControllers(this IServiceCollection services)
    {
        services.AddControllers(options =>
            // options.Filters.Add<JwtTokenAuthenticationFilter>();
            options.Filters.Add<ValidationFilter>());
        services.AddEndpointsApiExplorer();
        services.ConfigureFluentValidation();
    }

    /// <summary>
    /// Configures FluentValidation for automatic model validation with scoped lifetime.
    /// </summary>
    private static void ConfigureFluentValidation(this IServiceCollection services)
    {
        // Register all validators from the Business assembly with Scoped lifetime
        // Scoped lifetime is required for validators that inject repositories or other scoped services
        services.AddValidatorsFromAssemblyContaining<CreateProductDto>(ServiceLifetime.Scoped);

        // Configure FluentValidation to automatically validate models and populate ModelState
        services.AddFluentValidationAutoValidation();

        // Disable automatic model state validation since we're using FluentValidation and ValidationFilter
        services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
    }
}
