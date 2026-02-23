using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.IntegrationTests.Fixtures;

namespace WebShop.IntegrationTests.API;

/// <summary>
/// Base class for API integration tests. Provides shared WebAppFactory, HttpClient,
/// database reset, and DRY helper methods to create entities via the API.
/// Follows SRP: each derived class tests one controller/resource.
/// </summary>
public abstract class ApiIntegrationTestBase
{
    private const int HttpTimeoutSeconds = 30;

    protected readonly WebAppFactory Factory;
    protected readonly HttpClient Client;

    protected ApiIntegrationTestBase(WebAppFactory factory)
    {
        WebAppFactory.EnsureTestDatabaseExists();
        Factory = factory;
        Client = factory.CreateClient();
        Client.Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);
    }

    protected async Task ResetDatabaseAsync()
    {
        await Factory.ResetDatabaseAsync();
    }

    protected async Task<int> CreateCustomerAsync()
    {
        CreateCustomerDto dto = new()
        {
            FirstName = "Order",
            LastName = "Test",
            Email = $"order-{Guid.NewGuid():N}@test.example.com",
            Gender = "male"
        };
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/customers", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<CustomerDto>? result = await response.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        return result!.Data!.Id;
    }

    protected async Task<int> CreateAddressAsync(int customerId)
    {
        CreateAddressDto dto = new()
        {
            CustomerId = customerId,
            FirstName = "Ship",
            LastName = "User",
            Address1 = "456 Ship St",
            City = "ShipCity",
            Zip = "67890"
        };
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/addresses", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<AddressDto>? result = await response.Content.ReadFromJsonAsync<Response<AddressDto>>();
        return result!.Data!.Id;
    }

    protected async Task<int> CreateProductAsync()
    {
        CreateProductDto dto = new() { Name = $"Product-{Guid.NewGuid():N}", Category = "Apparel", Gender = "male" };
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/products", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ProductDto>? result = await response.Content.ReadFromJsonAsync<Response<ProductDto>>();
        return result!.Data!.Id;
    }

    protected async Task<int> CreateOrderAsync(int customerId, int addressId)
    {
        CreateOrderDto dto = new()
        {
            CustomerId = customerId,
            ShippingAddressId = addressId,
            ShippingCost = 9.99m
        };
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/orders", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<OrderDto>? result = await response.Content.ReadFromJsonAsync<Response<OrderDto>>();
        return result!.Data!.Id;
    }

    protected async Task<int> CreateLabelAsync(string? slugName = null)
    {
        string slug = slugName ?? $"slug-{Guid.NewGuid():N}";
        CreateLabelDto dto = new() { Name = $"Label-{slug}", SlugName = slug };
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/labels", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<LabelDto>? result = await response.Content.ReadFromJsonAsync<Response<LabelDto>>();
        return result!.Data!.Id;
    }

    protected async Task<int> CreateColorAsync(string? name = null)
    {
        CreateColorDto dto = new() { Name = name ?? $"Color-{Guid.NewGuid():N}", Rgb = "#FF0000" };
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/colors", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ColorDto>? result = await response.Content.ReadFromJsonAsync<Response<ColorDto>>();
        return result!.Data!.Id;
    }

    protected async Task<int> CreateSizeAsync()
    {
        CreateSizeDto dto = new() { Gender = "male", Category = "Apparel", SizeLabel = $"S{Guid.NewGuid():N}"[..8] };
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/sizes", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<SizeDto>? result = await response.Content.ReadFromJsonAsync<Response<SizeDto>>();
        return result!.Data!.Id;
    }

    protected async Task<int> CreateArticleAsync(int? productId = null, string? ean = null)
    {
        int pid = productId ?? await CreateProductAsync();
        int colorId = await CreateColorAsync();
        int sizeId = await CreateSizeAsync();
        string articleEan = ean ?? $"4{Guid.NewGuid():N}"[..13];
        CreateArticleDto dto = new()
        {
            ProductId = pid,
            Ean = articleEan,
            ColorId = colorId,
            Size = sizeId,
            OriginalPrice = 49.99m,
            CurrentlyActive = true
        };
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/articles", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ArticleDto>? result = await response.Content.ReadFromJsonAsync<Response<ArticleDto>>();
        return result!.Data!.Id;
    }

    protected async Task<int> CreateStockAsync(int? articleId = null)
    {
        int aid = articleId ?? await CreateArticleAsync();
        CreateStockDto dto = new() { ArticleId = aid, Count = 100 };
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/stocks", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<StockDto>? result = await response.Content.ReadFromJsonAsync<Response<StockDto>>();
        return result!.Data!.Id;
    }
}
