using System.Net;
using System.Text.Json;
using FluentAssertions;
using WebShop.IntegrationTests.Fixtures;
using Xunit;

namespace WebShop.IntegrationTests.API;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class HealthApiIntegrationTests(WebAppFactory factory) : ApiIntegrationTestBase(factory)
{

    [Fact]
    public async Task GetHealth_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy", because: "health endpoint should report healthy status");
        JsonDocument json = JsonDocument.Parse(content);
        json.RootElement.TryGetProperty("status", out _).Should().BeTrue();
    }
}
