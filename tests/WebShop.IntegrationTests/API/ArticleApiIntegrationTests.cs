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
public class ArticleApiIntegrationTests(WebAppFactory factory) : ApiIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetAll_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/articles");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();
        int productId = await CreateProductAsync();
        int colorId = await CreateColorAsync();
        int sizeId = await CreateSizeAsync();
        string ean = $"4{Guid.NewGuid():N}"[..13];
        CreateArticleDto createDto = new()
        {
            ProductId = productId,
            Ean = ean,
            ColorId = colorId,
            Size = sizeId,
            OriginalPrice = 49.99m,
            CurrentlyActive = true
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/articles", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ArticleDto>? result = await response.Content.ReadFromJsonAsync<Response<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_InvalidDto_Returns400()
    {
        await ResetDatabaseAsync();
        CreateArticleDto invalidDto = new() { ProductId = 0, Ean = "", ColorId = 0, Size = 0 };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/articles", invalidDto);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();
        int articleId = await CreateArticleAsync();

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/articles/{articleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<ArticleDto>? result = await response.Content.ReadFromJsonAsync<Response<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(articleId);
    }

    [Fact]
    public async Task GetByProductId_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int productId = await CreateProductAsync();
        await CreateArticleAsync(productId: productId);

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/articles/product/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetActive_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/articles/active");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetByEan_ValidEan_Returns200()
    {
        await ResetDatabaseAsync();
        string ean = $"4{Guid.NewGuid():N}"[..13];
        int articleId = await CreateArticleAsync(ean: ean);

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/articles/ean/{Uri.EscapeDataString(ean)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<ArticleDto>? result = await response.Content.ReadFromJsonAsync<Response<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Ean.Should().Be(ean);
    }

    [Fact]
    public async Task Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int articleId = await CreateArticleAsync();
        UpdateArticleDto updateDto = new() { Description = "Updated description", OriginalPrice = 59.99m };

        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/v1/articles/{articleId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();
        int articleId = await CreateArticleAsync();

        HttpResponseMessage response = await Client.DeleteAsync($"/api/v1/articles/{articleId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/articles/{articleId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/articles/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int articleId = await CreateArticleAsync();
        UpdateArticleDto patchDto = new() { Description = "Patched desc" };

        HttpResponseMessage response = await Client.PatchAsJsonAsync($"/api/v1/articles/{articleId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int productId = await CreateProductAsync();
        int colorId = await CreateColorAsync();
        int sizeId = await CreateSizeAsync();
        List<CreateArticleDto> dtos =
        [
            new() { ProductId = productId, Ean = $"4{Guid.NewGuid():N}"[..13], ColorId = colorId, Size = sizeId, OriginalPrice = 10m, CurrentlyActive = true },
            new() { ProductId = productId, Ean = $"4{Guid.NewGuid():N}"[..13], ColorId = colorId, Size = sizeId, OriginalPrice = 20m, CurrentlyActive = true }
        ];

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/articles/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateArticleAsync();
        int id2 = await CreateArticleAsync();
        List<BatchUpdateRequest<UpdateArticleDto>> updates = new()
        {
            new() { Id = id1, Data = new UpdateArticleDto { Description = "Desc1" } },
            new() { Id = id2, Data = new UpdateArticleDto { Description = "Desc2" } }
        };

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/v1/articles/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateArticleAsync();
        int id2 = await CreateArticleAsync();

        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/articles/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetByEan_InvalidEan_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/articles/ean/0000000000000");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
