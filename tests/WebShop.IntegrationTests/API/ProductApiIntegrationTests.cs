using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.IntegrationTests.Fixtures;
using Xunit;

namespace WebShop.IntegrationTests.API;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class ProductApiIntegrationTests(WebAppFactory factory) : ApiIntegrationTestBase(factory)
{

    [Fact]
    public async Task GetAll_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/products");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();
        CreateProductDto createDto = new() { Name = "Integration Product", Category = "Apparel", Gender = "male" };
        HttpResponseMessage createResponse = await Client.PostAsJsonAsync("/api/v1/products", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ProductDto>? createResult = await createResponse.Content.ReadFromJsonAsync<Response<ProductDto>>();
        int productId = createResult!.Data!.Id;

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<ProductDto>? result = await response.Content.ReadFromJsonAsync<Response<ProductDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(productId);
        result.Data.Name.Should().Be(createDto.Name);
    }

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();
        CreateProductDto createDto = new() { Name = "New Product", Category = "Apparel", Gender = "unisex" };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/products", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        Response<ProductDto>? result = await response.Content.ReadFromJsonAsync<Response<ProductDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetByCategory_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        CreateProductDto createDto = new() { Name = "Category Product", Category = "Footwear", Gender = "male" };
        await Client.PostAsJsonAsync("/api/v1/products", createDto);

        HttpResponseMessage response = await Client.GetAsync("/api/v1/products/category/Footwear");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetActive_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/products/active");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int productId = await CreateProductAsync();
        UpdateProductDto updateDto = new() { Name = "Updated Product", CurrentlyActive = true };

        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/v1/products/{productId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/products/{productId}");
        Response<ProductDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<ProductDto>>();
        getResult!.Data!.Name.Should().Be("Updated Product");
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();
        int productId = await CreateProductAsync();

        HttpResponseMessage response = await Client.DeleteAsync($"/api/v1/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/products/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_WithPagination_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/products?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Create_InvalidDto_Returns400()
    {
        await ResetDatabaseAsync();
        CreateProductDto invalidDto = new() { Name = "" };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/products", invalidDto);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int productId = await CreateProductAsync();
        UpdateProductDto patchDto = new UpdateProductDto { Name = "Patched Product" };

        HttpResponseMessage response = await Client.PatchAsJsonAsync($"/api/v1/products/{productId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        List<CreateProductDto> dtos = new()
        {
            new() { Name = "BatchP1", Category = "Apparel", Gender = "male" },
            new() { Name = "BatchP2", Category = "Footwear", Gender = "female" }
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/products/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateProductAsync();
        int id2 = await CreateProductAsync();
        List<BatchUpdateRequest<UpdateProductDto>> updates = new()
        {
            new() { Id = id1, Data = new UpdateProductDto { Name = "UpdatedP1" } },
            new() { Id = id2, Data = new UpdateProductDto { Name = "UpdatedP2" } }
        };

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/v1/products/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateProductAsync();
        int id2 = await CreateProductAsync();

        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/products/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetByCategory_ValidCategoryNoProducts_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/products/category/Luggage");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }
}
