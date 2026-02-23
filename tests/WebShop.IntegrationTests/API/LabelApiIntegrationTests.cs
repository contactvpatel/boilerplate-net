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
public class LabelApiIntegrationTests(WebAppFactory factory) : ApiIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetAll_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/labels");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();
        CreateLabelDto createDto = new() { Name = "Test Brand", SlugName = "test-brand" };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/labels", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<LabelDto>? result = await response.Content.ReadFromJsonAsync<Response<LabelDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_InvalidDto_Returns400()
    {
        await ResetDatabaseAsync();
        CreateLabelDto invalidDto = new() { Name = "", SlugName = "" };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/labels", invalidDto);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();
        int labelId = await CreateLabelAsync();

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/labels/{labelId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<LabelDto>? result = await response.Content.ReadFromJsonAsync<Response<LabelDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetBySlugName_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        string slug = $"slug-{Guid.NewGuid():N}";
        await CreateLabelAsync(slugName: slug);

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/labels/slug/{Uri.EscapeDataString(slug)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<LabelDto>? result = await response.Content.ReadFromJsonAsync<Response<LabelDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int labelId = await CreateLabelAsync();
        UpdateLabelDto updateDto = new() { Name = "Updated Brand", SlugName = "updated-brand" };

        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/v1/labels/{labelId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();
        int labelId = await CreateLabelAsync();

        HttpResponseMessage response = await Client.DeleteAsync($"/api/v1/labels/{labelId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/labels/{labelId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/labels/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetBySlugName_InvalidSlug_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/labels/slug/nonexistent-slug-12345");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int labelId = await CreateLabelAsync();
        UpdateLabelDto patchDto = new() { Name = "Patched Label" };

        HttpResponseMessage response = await Client.PatchAsJsonAsync($"/api/v1/labels/{labelId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        List<CreateLabelDto> dtos = new()
        {
            new() { Name = "BatchL1", SlugName = $"slug1-{Guid.NewGuid():N}" },
            new() { Name = "BatchL2", SlugName = $"slug2-{Guid.NewGuid():N}" }
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/labels/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateLabelAsync();
        int id2 = await CreateLabelAsync();
        List<BatchUpdateRequest<UpdateLabelDto>> updates = new()
        {
            new() { Id = id1, Data = new UpdateLabelDto { Name = "UpdatedL1" } },
            new() { Id = id2, Data = new UpdateLabelDto { Name = "UpdatedL2" } }
        };

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/v1/labels/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateLabelAsync();
        int id2 = await CreateLabelAsync();

        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/labels/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }
}
