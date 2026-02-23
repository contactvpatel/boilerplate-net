using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using WebShop.Api.Models;
using WebShop.IntegrationTests.Fixtures;
using Xunit;

namespace WebShop.IntegrationTests.API;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class CacheManagementApiIntegrationTests(WebAppFactory factory) : ApiIntegrationTestBase(factory)
{
    [Fact]
    public async Task ClearByKeys_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        var request = new { Keys = new[] { "test-key-1", "test-key-2" } };

        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/cache-management/keys") { Content = JsonContent.Create(request) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ClearByTag_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        var request = new { Tag = "test-tag" };

        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/cache-management/tag") { Content = JsonContent.Create(request) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ClearByTags_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        var request = new { Tags = new[] { "tag1", "tag2" } };

        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/cache-management/tags") { Content = JsonContent.Create(request) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task ClearByKey_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await Client.DeleteAsync("/api/v1/cache-management/key/test-key-123");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }
}
