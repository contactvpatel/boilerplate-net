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
public class ColorApiIntegrationTests(WebAppFactory factory) : ApiIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetAll_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/colors");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();
        CreateColorDto createDto = new() { Name = "Test Red", Rgb = "#FF0000" };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/colors", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ColorDto>? result = await response.Content.ReadFromJsonAsync<Response<ColorDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_InvalidDto_Returns400()
    {
        await ResetDatabaseAsync();
        CreateColorDto invalidDto = new() { Name = "", Rgb = "" };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/colors", invalidDto);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();
        int colorId = await CreateColorAsync();

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/colors/{colorId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<ColorDto>? result = await response.Content.ReadFromJsonAsync<Response<ColorDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetByName_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        string name = $"color-{Guid.NewGuid():N}";
        await CreateColorAsync(name: name);

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/colors/name/{Uri.EscapeDataString(name)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<ColorDto>? result = await response.Content.ReadFromJsonAsync<Response<ColorDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int colorId = await CreateColorAsync();
        UpdateColorDto updateDto = new() { Name = "Updated Red", Rgb = "#CC0000" };

        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/v1/colors/{colorId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();
        int colorId = await CreateColorAsync();

        HttpResponseMessage response = await Client.DeleteAsync($"/api/v1/colors/{colorId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/colors/{colorId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/colors/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByName_InvalidName_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/colors/name/NonexistentColorName123");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int colorId = await CreateColorAsync();
        UpdateColorDto patchDto = new() { Name = "Patched Color" };

        HttpResponseMessage response = await Client.PatchAsJsonAsync($"/api/v1/colors/{colorId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        List<CreateColorDto> dtos = new()
        {
            new() { Name = "BatchC1", Rgb = "#FF0000" },
            new() { Name = "BatchC2", Rgb = "#00FF00" }
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/colors/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateColorAsync();
        int id2 = await CreateColorAsync();
        List<BatchUpdateRequest<UpdateColorDto>> updates = new()
        {
            new() { Id = id1, Data = new UpdateColorDto { Name = "UpdatedC1" } },
            new() { Id = id2, Data = new UpdateColorDto { Name = "UpdatedC2" } }
        };

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/v1/colors/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateColorAsync();
        int id2 = await CreateColorAsync();

        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/colors/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }
}
