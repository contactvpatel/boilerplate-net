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
public class StockApiIntegrationTests(WebAppFactory factory) : ApiIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetAll_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/stocks");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();
        int articleId = await CreateArticleAsync();
        CreateStockDto createDto = new() { ArticleId = articleId, Count = 100 };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/stocks", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<StockDto>? result = await response.Content.ReadFromJsonAsync<Response<StockDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_InvalidDto_Returns400()
    {
        await ResetDatabaseAsync();
        CreateStockDto invalidDto = new() { ArticleId = 0, Count = -1 };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/stocks", invalidDto);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();
        int stockId = await CreateStockAsync();

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/stocks/{stockId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<StockDto>? result = await response.Content.ReadFromJsonAsync<Response<StockDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetByArticleId_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int articleId = await CreateArticleAsync();
        await CreateStockAsync(articleId);

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/stocks/article/{articleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<StockDto>? result = await response.Content.ReadFromJsonAsync<Response<StockDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetLowStock_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/stocks/low-stock?threshold=5");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int stockId = await CreateStockAsync();
        UpdateStockDto updateDto = new() { Count = 50 };

        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/v1/stocks/{stockId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();
        int stockId = await CreateStockAsync();

        HttpResponseMessage response = await Client.DeleteAsync($"/api/v1/stocks/{stockId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/stocks/{stockId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/stocks/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByArticleId_InvalidArticleId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/stocks/article/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int stockId = await CreateStockAsync();
        UpdateStockDto patchDto = new() { Count = 25 };

        HttpResponseMessage response = await Client.PatchAsJsonAsync($"/api/v1/stocks/{stockId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int articleId1 = await CreateArticleAsync();
        int articleId2 = await CreateArticleAsync();
        List<CreateStockDto> dtos = new()
        {
            new() { ArticleId = articleId1, Count = 50 },
            new() { ArticleId = articleId2, Count = 75 }
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/stocks/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateStockAsync();
        int id2 = await CreateStockAsync();
        List<BatchUpdateRequest<UpdateStockDto>> updates = new()
        {
            new() { Id = id1, Data = new UpdateStockDto { Count = 15 } },
            new() { Id = id2, Data = new UpdateStockDto { Count = 30 } }
        };

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/v1/stocks/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateStockAsync();
        int id2 = await CreateStockAsync();

        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/stocks/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }
}
