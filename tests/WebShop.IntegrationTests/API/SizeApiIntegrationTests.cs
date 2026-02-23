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
public class SizeApiIntegrationTests(WebAppFactory factory) : ApiIntegrationTestBase(factory)
{

    [Fact]
    public async Task GetAll_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/sizes");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();
        CreateSizeDto createDto = new() { Gender = "male", Category = "Apparel", SizeLabel = "M" };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/sizes", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<SizeDto>? result = await response.Content.ReadFromJsonAsync<Response<SizeDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_InvalidDto_Returns400()
    {
        await ResetDatabaseAsync();
        CreateSizeDto invalidDto = new() { Gender = "", Category = "", SizeLabel = "" };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/sizes", invalidDto);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();
        int sizeId = await CreateSizeAsync();

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/sizes/{sizeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<SizeDto>? result = await response.Content.ReadFromJsonAsync<Response<SizeDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetByGenderAndCategory_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/sizes/gender/male/category/Apparel");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int sizeId = await CreateSizeAsync();
        UpdateSizeDto updateDto = new() { SizeLabel = "L" };

        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/v1/sizes/{sizeId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();
        int sizeId = await CreateSizeAsync();

        HttpResponseMessage response = await Client.DeleteAsync($"/api/v1/sizes/{sizeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/sizes/{sizeId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/sizes/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int sizeId = await CreateSizeAsync();
        UpdateSizeDto patchDto = new() { SizeLabel = "XL" };

        HttpResponseMessage response = await Client.PatchAsJsonAsync($"/api/v1/sizes/{sizeId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        List<CreateSizeDto> dtos = new()
        {
            new() { Gender = "male", Category = "Apparel", SizeLabel = "XS" },
            new() { Gender = "female", Category = "Apparel", SizeLabel = "S" }
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/sizes/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateSizeAsync();
        int id2 = await CreateSizeAsync();
        List<BatchUpdateRequest<UpdateSizeDto>> updates = new()
        {
            new() { Id = id1, Data = new UpdateSizeDto { SizeLabel = "XXL" } },
            new() { Id = id2, Data = new UpdateSizeDto { SizeLabel = "M" } }
        };

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/v1/sizes/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateSizeAsync();
        int id2 = await CreateSizeAsync();

        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/sizes/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }
}
