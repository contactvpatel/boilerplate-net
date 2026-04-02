using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WebShop.Infrastructure.DbUp.Core;
using WebShop.Infrastructure.Extensions;

namespace WebShop.Infrastructure;

/// <summary>
/// Central entry point for registering all infrastructure services.
/// Orchestrates data access, caching, and HTTP client configuration.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers all infrastructure services: caching, data access (Dapper/repositories), HTTP clients, and external services.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInfrastructureCaching(configuration);
        services.AddInfrastructureDataAccess();

        // DbUp database migration configuration
        services.Configure<DatabaseMigrationOptions>(
            configuration.GetSection(DatabaseMigrationOptions.SectionName));
        services.AddSingleton<IStartupFilter, DatabaseMigrationInitFilter>();

        services.AddInfrastructureHttpClients(configuration);

        return services;
    }
}
