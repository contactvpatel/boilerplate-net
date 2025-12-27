using System.Reflection;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using WebShop.Business.DTOs;
using WebShop.Core.Entities;

namespace WebShop.Business.Mappings;

/// <summary>
/// Configures high-performance object mapping for all business entities.
/// Automatically converts between entities and DTOs with minimal overhead.
/// </summary>
/// <remarks>
/// This configuration ensures fast and consistent data transformation across the application,
/// supporting customer management, order processing, inventory tracking, and all business operations.
/// </remarks>
public static class MapsterConfiguration
{
    /// <summary>
    /// Sets up automatic mapping for all business entities and DTOs at application startup.
    /// </summary>
    public static IServiceCollection AddMapsterConfiguration(this IServiceCollection services)
    {
        // Scan current assembly for mapper configurations
        TypeAdapterConfig config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());

        // Enable compilation for better performance
        config.Compile();

        return services;
    }
}

/// <summary>
/// Configures data transformation for customer management operations.
/// Handles customer registration, profile updates, and customer data retrieval.
/// </summary>
public class CustomerMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Display customer information
        config.NewConfig<Customer, CustomerDto>();

        // Register new customers with active status
        config.NewConfig<CreateCustomerDto, Customer>()
            .Map(dest => dest.IsActive, src => true);

        // Update customer profiles (only updates provided fields)
        config.NewConfig<UpdateCustomerDto, Customer>()
            .IgnoreNullValues(true);
    }
}

/// <summary>
/// Configures data transformation for product catalog management.
/// Supports product browsing, catalog administration, and inventory operations.
/// </summary>
public class ProductMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Display product catalog
        config.NewConfig<Product, ProductDto>();

        // Add new products to catalog
        config.NewConfig<CreateProductDto, Product>()
            .Map(dest => dest.IsActive, src => true);

        // Update product information
        config.NewConfig<UpdateProductDto, Product>()
            .IgnoreNullValues(true);
    }
}

/// <summary>
/// Configures data transformation for order processing and management.
/// Handles order placement, order tracking, and order fulfillment operations.
/// </summary>
public class OrderMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Display order details and history
        config.NewConfig<Order, OrderDto>();

        // Create new customer orders
        config.NewConfig<CreateOrderDto, Order>()
            .Map(dest => dest.IsActive, src => true);

        // Update order status and details
        config.NewConfig<UpdateOrderDto, Order>()
            .IgnoreNullValues(true);
    }
}

/// <summary>
/// Configures data transformation for product variants and SKU management.
/// Manages specific product configurations (color/size combinations) and pricing.
/// </summary>
public class ArticleMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Display product variants and SKUs
        config.NewConfig<Article, ArticleDto>();

        // Add new product variants
        config.NewConfig<CreateArticleDto, Article>()
            .Map(dest => dest.IsActive, src => true);

        // Update variant details and pricing
        config.NewConfig<UpdateArticleDto, Article>()
            .IgnoreNullValues(true);
    }
}

/// <summary>
/// Configures data transformation for shipping and billing address management.
/// Supports customer address book, order fulfillment, and delivery operations.
/// </summary>
public class AddressMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Display customer addresses
        config.NewConfig<Address, AddressDto>();

        // Add new shipping/billing addresses
        config.NewConfig<CreateAddressDto, Address>()
            .Map(dest => dest.IsActive, src => true);

        // Update address information
        config.NewConfig<UpdateAddressDto, Address>()
            .IgnoreNullValues(true);
    }
}

/// <summary>
/// Configures data transformation for product color options.
/// Manages the color catalog used for product customization and filtering.
/// </summary>
public class ColorMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Display available product colors
        config.NewConfig<Color, ColorDto>();

        // Add new color options
        config.NewConfig<CreateColorDto, Color>()
            .Map(dest => dest.IsActive, src => true);

        // Update color information
        config.NewConfig<UpdateColorDto, Color>()
            .IgnoreNullValues(true);
    }
}

/// <summary>
/// Configures data transformation for product sizing options.
/// Manages the size catalog supporting multiple sizing standards (US, UK, EU).
/// </summary>
public class SizeMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Display available product sizes
        config.NewConfig<Size, SizeDto>();

        // Add new size options
        config.NewConfig<CreateSizeDto, Size>()
            .Map(dest => dest.IsActive, src => true);

        // Update size information
        config.NewConfig<UpdateSizeDto, Size>()
            .IgnoreNullValues(true);
    }
}

/// <summary>
/// Configures data transformation for product brands and labels.
/// Manages brand information used for product categorization and marketing.
/// </summary>
public class LabelMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Display product brands
        config.NewConfig<Label, LabelDto>();

        // Add new brand labels
        config.NewConfig<CreateLabelDto, Label>()
            .Map(dest => dest.IsActive, src => true);

        // Update brand information
        config.NewConfig<UpdateLabelDto, Label>()
            .IgnoreNullValues(true);
    }
}

/// <summary>
/// Configures data transformation for inventory and stock management.
/// Tracks product availability, stock levels, and warehouse operations.
/// </summary>
public class StockMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        // Display stock levels
        config.NewConfig<Stock, StockDto>();

        // Record new stock entries
        config.NewConfig<CreateStockDto, Stock>()
            .Map(dest => dest.IsActive, src => true);

        // Update inventory quantities
        config.NewConfig<UpdateStockDto, Stock>()
            .IgnoreNullValues(true);
    }
}
