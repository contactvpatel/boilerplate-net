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
public class CustomerApiIntegrationTests(WebAppFactory factory) : ApiIntegrationTestBase(factory)
{

    [Fact]
    public async Task GetAll_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/customers");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue(because: "payload must have Succeeded=true");
        result.Data.Should().NotBeNull(because: "payload must have Data");
    }

    [Fact]
    public async Task GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();
        CreateCustomerDto createDto = new()
        {
            FirstName = "Integration",
            LastName = "Test",
            Email = $"integration-{Guid.NewGuid():N}@test.example.com",
            Gender = "male"
        };
        HttpResponseMessage createResponse = await Client.PostAsJsonAsync("/api/v1/customers", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<CustomerDto>? createResult = await createResponse.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        int customerId = createResult!.Data!.Id;

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/customers/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<CustomerDto>? result = await response.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(customerId);
        result.Data.FirstName.Should().Be(createDto.FirstName);
        result.Data.LastName.Should().Be(createDto.LastName);
        result.Data.Email.Should().Be(createDto.Email);
    }

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();
        CreateCustomerDto createDto = new()
        {
            FirstName = "New",
            LastName = "Customer",
            Email = $"new-{Guid.NewGuid():N}@test.example.com",
            Gender = "male"
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/customers", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/customers/");
        Response<CustomerDto>? result = await response.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
        result.Data.FirstName.Should().Be(createDto.FirstName);
        result.Data.Email.Should().Be(createDto.Email);

        string locationPath = response.Headers.Location?.PathAndQuery ?? $"/api/v1/customers/{result.Data!.Id}";
        HttpResponseMessage getResponse = await Client.GetAsync(locationPath);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<CustomerDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        getResult!.Data!.Id.Should().Be(result.Data.Id);
        getResult.Data.Email.Should().Be(createDto.Email);
    }

    [Fact]
    public async Task Create_InvalidDto_Returns400()
    {
        await ResetDatabaseAsync();
        CreateCustomerDto createDto = new()
        {
            FirstName = "",
            LastName = "",
            Email = "invalid-email-format"
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/customers", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("succeeded", because: "error response should have standard structure");
    }

    [Fact]
    public async Task Create_DuplicateEmail_Returns400()
    {
        await ResetDatabaseAsync();
        string email = $"dup-{Guid.NewGuid():N}@test.example.com";
        CreateCustomerDto createDto = new()
        {
            FirstName = "First",
            LastName = "Customer",
            Email = email,
            Gender = "male"
        };
        HttpResponseMessage firstResponse = await Client.PostAsJsonAsync("/api/v1/customers", createDto);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        CreateCustomerDto duplicateDto = new()
        {
            FirstName = "Second",
            LastName = "Customer",
            Email = email,
            Gender = "female"
        };
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/customers", duplicateDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Response<CustomerDto>? result = await response.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Email", because: "duplicate email should return business rule error");
    }

    [Fact]
    public async Task GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/customers/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByEmail_ValidEmail_Returns200()
    {
        await ResetDatabaseAsync();
        string email = $"email-{Guid.NewGuid():N}@test.example.com";
        CreateCustomerDto createDto = new() { FirstName = "Email", LastName = "Test", Email = email, Gender = "male" };
        await Client.PostAsJsonAsync("/api/v1/customers", createDto);

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/customers/email/{Uri.EscapeDataString(email)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<CustomerDto>? result = await response.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        UpdateCustomerDto updateDto = new() { FirstName = "Updated", LastName = "Name", Email = $"updated-{Guid.NewGuid():N}@test.example.com", Gender = "female" };

        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/v1/customers/{customerId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/customers/{customerId}");
        Response<CustomerDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        getResult!.Data!.FirstName.Should().Be("Updated");
        getResult.Data.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();

        HttpResponseMessage response = await Client.DeleteAsync($"/api/v1/customers/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/customers/{customerId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_WithPagination_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/customers?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        UpdateCustomerDto updateDto = new() { FirstName = "X", LastName = "Y", Email = "x@y.com", Gender = "male" };
        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/v1/customers/999999", updateDto);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.DeleteAsync("/api/v1/customers/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        UpdateCustomerDto patchDto = new UpdateCustomerDto { FirstName = "Patched" };
        HttpResponseMessage response = await Client.PatchAsJsonAsync($"/api/v1/customers/{customerId}", patchDto);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task CreateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        List<CreateCustomerDto> dtos = new()
        {
            new() { FirstName = "B1", LastName = "Batch", Email = $"b1-{Guid.NewGuid():N}@test.example.com", Gender = "male" },
            new() { FirstName = "B2", LastName = "Batch", Email = $"b2-{Guid.NewGuid():N}@test.example.com", Gender = "female" }
        };
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/customers/batch", dtos);
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateCustomerAsync();
        int id2 = await CreateCustomerAsync();
        List<BatchUpdateRequest<UpdateCustomerDto>> updates = new()
        {
            new() { Id = id1, Data = new UpdateCustomerDto { FirstName = "U1", LastName = "L1", Email = $"u1-{Guid.NewGuid():N}@test.example.com", Gender = "male" } },
            new() { Id = id2, Data = new UpdateCustomerDto { FirstName = "U2", LastName = "L2", Email = $"u2-{Guid.NewGuid():N}@test.example.com", Gender = "female" } }
        };
        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/v1/customers/batch", updates);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateCustomerAsync();
        int id2 = await CreateCustomerAsync();
        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/customers/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetByEmail_InvalidEmail_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/customers/email/nonexistent@example.com");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_EmptyBatch_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/customers/batch", new List<CreateCustomerDto>());
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }
}
