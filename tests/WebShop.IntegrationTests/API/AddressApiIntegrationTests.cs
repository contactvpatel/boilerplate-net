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
public class AddressApiIntegrationTests(WebAppFactory factory) : ApiIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetAll_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/addresses");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        CreateAddressDto createDto = new()
        {
            CustomerId = customerId,
            FirstName = "Test",
            LastName = "User",
            Address1 = "123 Test St",
            City = "TestCity",
            Zip = "12345"
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/addresses", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<AddressDto>? result = await response.Content.ReadFromJsonAsync<Response<AddressDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_InvalidDto_Returns400()
    {
        await ResetDatabaseAsync();
        CreateAddressDto invalidDto = new() { CustomerId = 0, FirstName = "", LastName = "", Address1 = "", City = "", Zip = "" };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/addresses", invalidDto);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/addresses/{addressId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<AddressDto>? result = await response.Content.ReadFromJsonAsync<Response<AddressDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(addressId);
    }

    [Fact]
    public async Task GetByCustomerId_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        await CreateAddressAsync(customerId);

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/addresses/customer/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        UpdateAddressDto updateDto = new() { FirstName = "Updated", LastName = "Name", Address1 = "999 New St", City = "NewCity", Zip = "99999" };

        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/v1/addresses/{addressId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/addresses/{addressId}");
        Response<AddressDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<AddressDto>>();
        getResult!.Data!.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);

        HttpResponseMessage response = await Client.DeleteAsync($"/api/v1/addresses/{addressId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/addresses/{addressId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/addresses/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        UpdateAddressDto patchDto = new() { City = "PatchedCity" };

        HttpResponseMessage response = await Client.PatchAsJsonAsync($"/api/v1/addresses/{addressId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/addresses/{addressId}");
        Response<AddressDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<AddressDto>>();
        getResult!.Data!.City.Should().Be("PatchedCity");
    }

    [Fact]
    public async Task Patch_ValidDtoWithCustomerIdChange_Returns204()
    {
        await ResetDatabaseAsync();
        int customer1Id = await CreateCustomerAsync();
        int customer2Id = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customer1Id);
        UpdateAddressDto patchDto = new() { CustomerId = customer2Id };

        HttpResponseMessage response = await Client.PatchAsJsonAsync($"/api/v1/addresses/{addressId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/addresses/{addressId}");
        Response<AddressDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<AddressDto>>();
        getResult!.Data!.CustomerId.Should().Be(customer2Id);
    }

    [Fact]
    public async Task UpdateBatch_WithInvalidCustomerId_SkipsInvalidAddress()
    {
        await ResetDatabaseAsync();
        int customer1Id = await CreateCustomerAsync();
        int customer2Id = await CreateCustomerAsync();
        int address1Id = await CreateAddressAsync(customer1Id);
        int address2Id = await CreateAddressAsync(customer2Id);

        List<BatchUpdateRequest<UpdateAddressDto>> updates = new()
        {
            new() { Id = address1Id, Data = new UpdateAddressDto { CustomerId = customer2Id, FirstName = "Batch1", LastName = "User", Address1 = "1 St", City = "C1", Zip = "11111" } },
            new() { Id = address2Id, Data = new UpdateAddressDto { CustomerId = 999999, FirstName = "Batch2", LastName = "User", Address1 = "2 St", City = "C2", Zip = "22222" } }
        };

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/v1/addresses/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<IReadOnlyList<AddressDto>>? result = await response.Content.ReadFromJsonAsync<Response<IReadOnlyList<AddressDto>>>();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Count.Should().Be(1);
        result.Data[0].Id.Should().Be(address1Id);
        result.Data[0].CustomerId.Should().Be(customer2Id);
    }

    [Fact]
    public async Task CreateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        List<CreateAddressDto> dtos = new()
        {
            new() { CustomerId = customerId, FirstName = "A1", LastName = "Batch", Address1 = "1 St", City = "C1", Zip = "11111" },
            new() { CustomerId = customerId, FirstName = "A2", LastName = "Batch", Address1 = "2 St", City = "C2", Zip = "22222" }
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/addresses/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int id1 = await CreateAddressAsync(customerId);
        int id2 = await CreateAddressAsync(customerId);
        List<BatchUpdateRequest<UpdateAddressDto>> updates = new()
        {
            new() { Id = id1, Data = new UpdateAddressDto { City = "City1" } },
            new() { Id = id2, Data = new UpdateAddressDto { City = "City2" } }
        };

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/v1/addresses/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int id1 = await CreateAddressAsync(customerId);
        int id2 = await CreateAddressAsync(customerId);

        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/addresses/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }
}
