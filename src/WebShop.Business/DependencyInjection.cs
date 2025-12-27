using Microsoft.Extensions.DependencyInjection;
using WebShop.Business.Mappings;
using WebShop.Business.Services;
using WebShop.Business.Services.Interfaces;

namespace WebShop.Business;

public static class DependencyInjection
{
    public static IServiceCollection AddBusinessServices(this IServiceCollection services)
    {
        // Configure Mapster mappings for optimized performance
        services.AddMapsterConfiguration();

        // Register services
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAddressService, AddressService>();
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IArticleService, ArticleService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<ILabelService, LabelService>();
        services.AddScoped<IColorService, ColorService>();
        services.AddScoped<ISizeService, SizeService>();
        services.AddScoped<IStockService, StockService>();

        // Register SSO service (business layer wraps core service)
        services.AddScoped<ISsoService, SsoService>();

        // Register MIS service (business layer wraps core service)
        services.AddScoped<IMisService, MisService>();

        // Register ASM service (business layer wraps core service)
        services.AddScoped<IAsmService, AsmService>();

        return services;
    }
}

