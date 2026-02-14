using Microsoft.Extensions.DependencyInjection;
using WebShop.Core.Interfaces;
using WebShop.Core.Interfaces.Base;
using WebShop.Infrastructure.Helpers;
using WebShop.Infrastructure.Interfaces;
using WebShop.Infrastructure.Repositories;

namespace WebShop.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering data access infrastructure (Dapper, repositories, unit of work).
/// </summary>
public static class DataAccessExtensions
{
    /// <summary>
    /// Registers Dapper connection factory, transaction manager, unit of work, and all repositories.
    /// </summary>
    public static IServiceCollection AddInfrastructureDataAccess(this IServiceCollection services)
    {
        // Register Dapper connection factory (for Dapper repositories)
        services.AddSingleton<IDapperConnectionFactory, DapperConnectionFactory>();

        // Register Dapper transaction manager (scoped for request-based transactions)
        services.AddScoped<IDapperTransactionManager, DapperTransactionManager>();

        // Register unit of work for batch operations (shares transaction manager with repositories)
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register repositories (all using Dapper)
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IArticleRepository, ArticleRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOrderPositionRepository, OrderPositionRepository>();
        services.AddScoped<ILabelRepository, LabelRepository>();
        services.AddScoped<IColorRepository, ColorRepository>();
        services.AddScoped<ISizeRepository, SizeRepository>();
        services.AddScoped<IStockRepository, StockRepository>();

        return services;
    }
}
