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
public class OrderApiIntegrationTests(WebAppFactory factory) : ApiIntegrationTestBase(factory)
{
    [Fact]
    public async Task GetAll_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/orders");
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
        int addressId = await CreateAddressAsync(customerId);
        CreateOrderDto createDto = new()
        {
            CustomerId = customerId,
            ShippingAddressId = addressId,
            ShippingCost = 9.99m
        };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/orders", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<OrderDto>? result = await response.Content.ReadFromJsonAsync<Response<OrderDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Create_InvalidDto_Returns400()
    {
        await ResetDatabaseAsync();
        CreateOrderDto invalidDto = new() { CustomerId = 0, ShippingAddressId = 0, ShippingCost = -1m };

        HttpResponseMessage response = await Client.PostAsJsonAsync("/api/v1/orders", invalidDto);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        int orderId = await CreateOrderAsync(customerId, addressId);

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/orders/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<OrderDto>? result = await response.Content.ReadFromJsonAsync<Response<OrderDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task GetByCustomerId_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        await CreateOrderAsync(customerId, addressId);

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/orders/customer/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetByDateRange_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        string start = "2020-01-01T00:00:00Z";
        string end = "2030-12-31T23:59:59Z";

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/orders/date-range?startDate={Uri.EscapeDataString(start)}&endDate={Uri.EscapeDataString(end)}");

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
        int orderId = await CreateOrderAsync(customerId, addressId);
        UpdateOrderDto updateDto = new() { Total = 99.99m, ShippingCost = 5.00m };

        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/v1/orders/{orderId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        int orderId = await CreateOrderAsync(customerId, addressId);

        HttpResponseMessage response = await Client.DeleteAsync($"/api/v1/orders/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpResponseMessage getResponse = await Client.GetAsync($"/api/v1/orders/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/orders/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAll_WithPagination_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/orders?page=1&pageSize=10");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetByDateRange_InvalidRange_Returns400()
    {
        await ResetDatabaseAsync();
        string start = "2024-12-31T23:59:59Z";
        string end = "2024-01-01T00:00:00Z";

        HttpResponseMessage response = await Client.GetAsync($"/api/v1/orders/date-range?startDate={Uri.EscapeDataString(start)}&endDate={Uri.EscapeDataString(end)}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        int orderId = await CreateOrderAsync(customerId, addressId);
        UpdateOrderDto patchDto = new() { Total = 88.88m };

        HttpResponseMessage response = await Client.PatchAsJsonAsync($"/api/v1/orders/{orderId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task UpdateBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        int id1 = await CreateOrderAsync(customerId, addressId);
        int id2 = await CreateOrderAsync(customerId, addressId);
        List<BatchUpdateRequest<UpdateOrderDto>> updates = new()
        {
            new() { Id = id1, Data = new UpdateOrderDto { Total = 11.11m } },
            new() { Id = id2, Data = new UpdateOrderDto { Total = 22.22m } }
        };

        HttpResponseMessage response = await Client.PutAsJsonAsync("/api/v1/orders/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteBatch_WhenCalled_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        int id1 = await CreateOrderAsync(customerId, addressId);
        int id2 = await CreateOrderAsync(customerId, addressId);

        HttpResponseMessage response = await Client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/orders/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task GetAll_NonPaginated_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await Client.GetAsync("/api/v1/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }
}
