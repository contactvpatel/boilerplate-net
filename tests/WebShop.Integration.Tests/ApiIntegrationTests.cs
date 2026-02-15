using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using Xunit;

namespace WebShop.Integration.Tests;

/// <summary>
/// PR-level integration tests for core API endpoints.
/// Uses WebApplicationFactory with local PostgreSQL (no Docker).
/// Requires: PostgreSQL running locally with database 'webshop_test' (or set INTEGRATION_TEST_DB_* env vars).
/// Run: dotnet test tests/WebShop.Integration.Tests --filter "Category=Integration"
/// </summary>
[Trait("Category", TestCategories.Integration)]
public class ApiIntegrationTests : IClassFixture<WebAppFactory>
{
    private const int HttpTimeoutSeconds = 30;

    private readonly WebAppFactory _factory;
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebAppFactory factory)
    {
        WebAppFactory.EnsureTestDatabaseExists();
        _factory = factory;
        _client = factory.CreateClient();
        _client.Timeout = TimeSpan.FromSeconds(HttpTimeoutSeconds);
    }

    private async Task ResetDatabaseAsync()
    {
        await _factory.ResetDatabaseAsync();
    }

    #region Health Tests


    [Fact]
    public async Task Health_Returns200()
    {
        await ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy", because: "health endpoint should report healthy status");
        // Payload structure: keys exist
        JsonDocument json = JsonDocument.Parse(content);
        json.RootElement.TryGetProperty("status", out _).Should().BeTrue();
    }

    #endregion

    #region Customers Tests

    [Fact]
    public async Task Customers_GetAll_Returns200()
    {
        await ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/v1/customers");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue(because: "payload must have Succeeded=true");
        result.Data.Should().NotBeNull(because: "payload must have Data");
    }

    [Fact]
    public async Task Customers_GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();

        // Arrange - create a customer first
        CreateCustomerDto createDto = new()
        {
            FirstName = "Integration",
            LastName = "Test",
            Email = $"integration-{Guid.NewGuid():N}@test.example.com",
            Gender = "male"
        };
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/v1/customers", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<CustomerDto>? createResult = await createResponse.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        int customerId = createResult!.Data!.Id;

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/customers/{customerId}");

        // Assert
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
    public async Task Customers_Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();

        // Arrange
        CreateCustomerDto createDto = new()
        {
            FirstName = "New",
            LastName = "Customer",
            Email = $"new-{Guid.NewGuid():N}@test.example.com",
            Gender = "male"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/customers", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.Location!.ToString().Should().Contain("/api/v1/customers/");
        Response<CustomerDto>? result = await response.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
        result.Data.FirstName.Should().Be(createDto.FirstName);
        result.Data.Email.Should().Be(createDto.Email);

        // Verify data landed in DB: GET the created resource (use path from Location for relative URLs)
        string locationPath = response.Headers.Location?.PathAndQuery ?? $"/api/v1/customers/{result.Data!.Id}";
        HttpResponseMessage getResponse = await _client.GetAsync(locationPath);
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<CustomerDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        getResult!.Data!.Id.Should().Be(result.Data.Id);
        getResult.Data.Email.Should().Be(createDto.Email);
    }

    [Fact]
    public async Task Customers_Create_InvalidDto_Returns400()
    {
        await ResetDatabaseAsync();

        // Arrange - empty required fields
        CreateCustomerDto createDto = new()
        {
            FirstName = "",
            LastName = "",
            Email = "invalid-email-format"
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/customers", createDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        string content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("succeeded", because: "error response should have standard structure");
    }

    [Fact]
    public async Task Customers_Create_DuplicateEmail_Returns400()
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

        HttpResponseMessage firstResponse = await _client.PostAsJsonAsync("/api/v1/customers", createDto);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        CreateCustomerDto duplicateDto = new()
        {
            FirstName = "Second",
            LastName = "Customer",
            Email = email,
            Gender = "female"
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/customers", duplicateDto);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        Response<CustomerDto>? result = await response.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeFalse();
        result.Message.Should().Contain("Email", because: "duplicate email should return business rule error");
    }

    [Fact]
    public async Task Customers_GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/v1/customers/999999");

        // Assert - 404 does not return 500
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Customers_GetByEmail_ValidEmail_Returns200()
    {
        await ResetDatabaseAsync();

        string email = $"email-{Guid.NewGuid():N}@test.example.com";
        CreateCustomerDto createDto = new() { FirstName = "Email", LastName = "Test", Email = email, Gender = "male" };
        await _client.PostAsJsonAsync("/api/v1/customers", createDto);

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/customers/email/{Uri.EscapeDataString(email)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<CustomerDto>? result = await response.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Email.Should().Be(email);
    }

    [Fact]
    public async Task Customers_Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();

        int customerId = await CreateCustomerAsync();
        UpdateCustomerDto updateDto = new() { FirstName = "Updated", LastName = "Name", Email = $"updated-{Guid.NewGuid():N}@test.example.com", Gender = "female" };

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/customers/{customerId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/customers/{customerId}");
        Response<CustomerDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        getResult!.Data!.FirstName.Should().Be("Updated");
        getResult.Data.LastName.Should().Be("Name");
    }

    [Fact]
    public async Task Customers_Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();

        int customerId = await CreateCustomerAsync();

        HttpResponseMessage response = await _client.DeleteAsync($"/api/v1/customers/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/customers/{customerId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Customers_GetAll_WithPagination_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/customers?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    #endregion

    #region Products Tests

    [Fact]
    public async Task Products_GetAll_Returns200()
    {
        await ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await _client.GetAsync("/api/v1/products");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Products_GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();

        // Arrange - create product first
        CreateProductDto createDto = new() { Name = "Integration Product", Category = "Apparel", Gender = "male" };
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync("/api/v1/products", createDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ProductDto>? createResult = await createResponse.Content.ReadFromJsonAsync<Response<ProductDto>>();
        int productId = createResult!.Data!.Id;

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/api/v1/products/{productId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<ProductDto>? result = await response.Content.ReadFromJsonAsync<Response<ProductDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(productId);
        result.Data.Name.Should().Be(createDto.Name);
    }

    [Fact]
    public async Task Products_Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();

        CreateProductDto createDto = new() { Name = "New Product", Category = "Apparel", Gender = "unisex" };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/products", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();
        Response<ProductDto>? result = await response.Content.ReadFromJsonAsync<Response<ProductDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Products_GetByCategory_Returns200()
    {
        await ResetDatabaseAsync();

        CreateProductDto createDto = new() { Name = "Category Product", Category = "Footwear", Gender = "male" };
        await _client.PostAsJsonAsync("/api/v1/products", createDto);

        HttpResponseMessage response = await _client.GetAsync("/api/v1/products/category/Footwear");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Products_GetActive_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/products/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Products_Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();

        int productId = await CreateProductAsync();
        UpdateProductDto updateDto = new() { Name = "Updated Product", CurrentlyActive = true };

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/products/{productId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/products/{productId}");
        Response<ProductDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<ProductDto>>();
        getResult!.Data!.Name.Should().Be("Updated Product");
    }

    [Fact]
    public async Task Products_Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();

        int productId = await CreateProductAsync();

        HttpResponseMessage response = await _client.DeleteAsync($"/api/v1/products/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/products/{productId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Products_GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/products/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Products_GetAll_WithPagination_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/products?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Orders Tests

    [Fact]
    public async Task Orders_GetAll_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Orders_Create_ValidDto_Returns201()
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

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/orders", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<OrderDto>? result = await response.Content.ReadFromJsonAsync<Response<OrderDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Orders_GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();

        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        int orderId = await CreateOrderAsync(customerId, addressId);

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/orders/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<OrderDto>? result = await response.Content.ReadFromJsonAsync<Response<OrderDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task Orders_GetByCustomerId_Returns200()
    {
        await ResetDatabaseAsync();

        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        await CreateOrderAsync(customerId, addressId);

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/orders/customer/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Orders_GetByDateRange_Returns200()
    {
        await ResetDatabaseAsync();

        string start = "2020-01-01T00:00:00Z";
        string end = "2030-12-31T23:59:59Z";

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/orders/date-range?startDate={Uri.EscapeDataString(start)}&endDate={Uri.EscapeDataString(end)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Orders_Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();

        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        int orderId = await CreateOrderAsync(customerId, addressId);
        UpdateOrderDto updateDto = new() { Total = 99.99m, ShippingCost = 5.00m };

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/orders/{orderId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Orders_Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();

        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        int orderId = await CreateOrderAsync(customerId, addressId);

        HttpResponseMessage response = await _client.DeleteAsync($"/api/v1/orders/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/orders/{orderId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Orders_GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/orders/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Orders_GetAll_WithPagination_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/orders?page=1&pageSize=10");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Addresses Tests

    [Fact]
    public async Task Addresses_GetAll_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/addresses");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Addresses_Create_ValidDto_Returns201()
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

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/addresses", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<AddressDto>? result = await response.Content.ReadFromJsonAsync<Response<AddressDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Addresses_GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();

        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/addresses/{addressId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<AddressDto>? result = await response.Content.ReadFromJsonAsync<Response<AddressDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(addressId);
    }

    [Fact]
    public async Task Addresses_GetByCustomerId_Returns200()
    {
        await ResetDatabaseAsync();

        int customerId = await CreateCustomerAsync();
        await CreateAddressAsync(customerId);

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/addresses/customer/{customerId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Addresses_Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();

        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        UpdateAddressDto updateDto = new() { FirstName = "Updated", LastName = "Name", Address1 = "999 New St", City = "NewCity", Zip = "99999" };

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/addresses/{addressId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/addresses/{addressId}");
        Response<AddressDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<AddressDto>>();
        getResult!.Data!.FirstName.Should().Be("Updated");
    }

    [Fact]
    public async Task Addresses_Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();

        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);

        HttpResponseMessage response = await _client.DeleteAsync($"/api/v1/addresses/{addressId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/addresses/{addressId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Addresses_GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/addresses/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Addresses_Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();

        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);

        // PATCH with only City changed - exercises PartialUpdateHelper.ApplyIfChanged
        UpdateAddressDto patchDto = new() { City = "PatchedCity" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/v1/addresses/{addressId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/addresses/{addressId}");
        Response<AddressDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<AddressDto>>();
        getResult!.Data!.City.Should().Be("PatchedCity");
    }

    [Fact]
    public async Task Addresses_Patch_ValidDto_ChangeCustomerId_Returns204()
    {
        await ResetDatabaseAsync();

        int customer1Id = await CreateCustomerAsync();
        int customer2Id = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customer1Id);

        // PATCH with CustomerId change to another valid customer - exercises AddressService branch
        UpdateAddressDto patchDto = new() { CustomerId = customer2Id };

        HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/v1/addresses/{addressId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/addresses/{addressId}");
        Response<AddressDto>? getResult = await getResponse.Content.ReadFromJsonAsync<Response<AddressDto>>();
        getResult!.Data!.CustomerId.Should().Be(customer2Id);
    }

    [Fact]
    public async Task Addresses_UpdateBatch_WithInvalidCustomerId_SkipsInvalidAddress()
    {
        await ResetDatabaseAsync();

        int customer1Id = await CreateCustomerAsync();
        int customer2Id = await CreateCustomerAsync();
        int address1Id = await CreateAddressAsync(customer1Id);
        int address2Id = await CreateAddressAsync(customer2Id);

        // Batch update: address1 gets valid CustomerId change; address2 gets invalid CustomerId (999999)
        BatchUpdateRequest<UpdateAddressDto>[] updates = new[]
        {
            new BatchUpdateRequest<UpdateAddressDto> { Id = address1Id, Data = new UpdateAddressDto { CustomerId = customer2Id, FirstName = "Batch1", LastName = "User", Address1 = "1 St", City = "C1", Zip = "11111" } },
            new BatchUpdateRequest<UpdateAddressDto> { Id = address2Id, Data = new UpdateAddressDto { CustomerId = 999999, FirstName = "Batch2", LastName = "User", Address1 = "2 St", City = "C2", Zip = "22222" } }
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/v1/addresses/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<IReadOnlyList<AddressDto>>? result = await response.Content.ReadFromJsonAsync<Response<IReadOnlyList<AddressDto>>>();
        result!.Succeeded.Should().BeTrue();
        // Address with invalid CustomerId is skipped - only address1 is updated
        result.Data!.Count.Should().Be(1);
        result.Data[0].Id.Should().Be(address1Id);
        result.Data[0].CustomerId.Should().Be(customer2Id);
    }

    #endregion

    #region Articles Tests

    [Fact]
    public async Task Articles_GetAll_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/articles");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Articles_Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();

        int productId = await CreateProductAsync();
        int colorId = await CreateColorAsync();
        int sizeId = await CreateSizeAsync();
        CreateArticleDto createDto = new()
        {
            ProductId = productId,
            Ean = $"4{Guid.NewGuid():N}".Substring(0, 13),
            ColorId = colorId,
            Size = sizeId,
            OriginalPrice = 49.99m,
            CurrentlyActive = true
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/articles", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ArticleDto>? result = await response.Content.ReadFromJsonAsync<Response<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Articles_GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();

        int articleId = await CreateArticleAsync();

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/articles/{articleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<ArticleDto>? result = await response.Content.ReadFromJsonAsync<Response<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().Be(articleId);
    }

    [Fact]
    public async Task Articles_GetByProductId_Returns200()
    {
        await ResetDatabaseAsync();

        int productId = await CreateProductAsync();
        await CreateArticleAsync(productId: productId);

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/articles/product/{productId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Articles_GetActive_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/articles/active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Articles_GetByEan_ValidEan_Returns200()
    {
        await ResetDatabaseAsync();

        string ean = $"4{Guid.NewGuid():N}".Substring(0, 13);
        int articleId = await CreateArticleAsync(ean: ean);

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/articles/ean/{Uri.EscapeDataString(ean)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<ArticleDto>? result = await response.Content.ReadFromJsonAsync<Response<ArticleDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Ean.Should().Be(ean);
    }

    [Fact]
    public async Task Articles_Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();

        int articleId = await CreateArticleAsync();
        UpdateArticleDto updateDto = new() { Description = "Updated description", OriginalPrice = 59.99m };

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/articles/{articleId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Articles_Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();

        int articleId = await CreateArticleAsync();

        HttpResponseMessage response = await _client.DeleteAsync($"/api/v1/articles/{articleId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/articles/{articleId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Articles_GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/articles/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Labels, Colors, Sizes Tests

    [Fact]
    public async Task Labels_GetAll_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/labels");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Labels_Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();

        CreateLabelDto createDto = new() { Name = "Test Brand", SlugName = "test-brand" };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/labels", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<LabelDto>? result = await response.Content.ReadFromJsonAsync<Response<LabelDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Labels_GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();

        int labelId = await CreateLabelAsync();

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/labels/{labelId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<LabelDto>? result = await response.Content.ReadFromJsonAsync<Response<LabelDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Labels_GetBySlugName_Returns200()
    {
        await ResetDatabaseAsync();

        string slug = $"slug-{Guid.NewGuid():N}";
        int labelId = await CreateLabelAsync(slugName: slug);

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/labels/slug/{Uri.EscapeDataString(slug)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<LabelDto>? result = await response.Content.ReadFromJsonAsync<Response<LabelDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Labels_Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();

        int labelId = await CreateLabelAsync();
        UpdateLabelDto updateDto = new() { Name = "Updated Brand", SlugName = "updated-brand" };

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/labels/{labelId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Labels_Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();

        int labelId = await CreateLabelAsync();

        HttpResponseMessage response = await _client.DeleteAsync($"/api/v1/labels/{labelId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/labels/{labelId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Colors_GetAll_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/colors");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Colors_Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();

        CreateColorDto createDto = new() { Name = "Test Red", Rgb = "#FF0000" };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/colors", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ColorDto>? result = await response.Content.ReadFromJsonAsync<Response<ColorDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Colors_GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();

        int colorId = await CreateColorAsync();

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/colors/{colorId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<ColorDto>? result = await response.Content.ReadFromJsonAsync<Response<ColorDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Colors_GetByName_Returns200()
    {
        await ResetDatabaseAsync();

        string name = $"color-{Guid.NewGuid():N}";
        int colorId = await CreateColorAsync(name: name);

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/colors/name/{Uri.EscapeDataString(name)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<ColorDto>? result = await response.Content.ReadFromJsonAsync<Response<ColorDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Colors_Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();

        int colorId = await CreateColorAsync();
        UpdateColorDto updateDto = new() { Name = "Updated Red", Rgb = "#CC0000" };

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/colors/{colorId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Colors_Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();

        int colorId = await CreateColorAsync();

        HttpResponseMessage response = await _client.DeleteAsync($"/api/v1/colors/{colorId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/colors/{colorId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Sizes_GetAll_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/sizes");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Sizes_Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();

        CreateSizeDto createDto = new() { Gender = "male", Category = "Apparel", SizeLabel = "M" };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/sizes", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<SizeDto>? result = await response.Content.ReadFromJsonAsync<Response<SizeDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Sizes_GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();

        int sizeId = await CreateSizeAsync();

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/sizes/{sizeId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<SizeDto>? result = await response.Content.ReadFromJsonAsync<Response<SizeDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Sizes_GetByGenderAndCategory_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/sizes/gender/male/category/Apparel");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Sizes_Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();

        int sizeId = await CreateSizeAsync();
        UpdateSizeDto updateDto = new() { SizeLabel = "L" };

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/sizes/{sizeId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Sizes_Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();

        int sizeId = await CreateSizeAsync();

        HttpResponseMessage response = await _client.DeleteAsync($"/api/v1/sizes/{sizeId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/sizes/{sizeId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stock_GetAll_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/stocks");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Stock_Create_ValidDto_Returns201()
    {
        await ResetDatabaseAsync();

        int articleId = await CreateArticleAsync();
        CreateStockDto createDto = new() { ArticleId = articleId, Count = 100 };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/stocks", createDto);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<StockDto>? result = await response.Content.ReadFromJsonAsync<Response<StockDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.Data!.Id.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Stock_GetById_ValidId_Returns200()
    {
        await ResetDatabaseAsync();

        int stockId = await CreateStockAsync();

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/stocks/{stockId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<StockDto>? result = await response.Content.ReadFromJsonAsync<Response<StockDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Stock_GetByArticleId_Returns200()
    {
        await ResetDatabaseAsync();

        int articleId = await CreateArticleAsync();
        await CreateStockAsync(articleId);

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/stocks/article/{articleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<StockDto>? result = await response.Content.ReadFromJsonAsync<Response<StockDto>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Stock_GetLowStock_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/stocks/low-stock?threshold=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Stock_Update_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();

        int stockId = await CreateStockAsync();
        UpdateStockDto updateDto = new() { Count = 50 };

        HttpResponseMessage response = await _client.PutAsJsonAsync($"/api/v1/stocks/{stockId}", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Stock_Delete_ValidId_Returns204()
    {
        await ResetDatabaseAsync();

        int stockId = await CreateStockAsync();

        HttpResponseMessage response = await _client.DeleteAsync($"/api/v1/stocks/{stockId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        HttpResponseMessage getResponse = await _client.GetAsync($"/api/v1/stocks/{stockId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stock_GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.GetAsync("/api/v1/stocks/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Error Path Tests (400, Validation)

    [Fact]
    public async Task Products_Create_InvalidDto_Returns400()
    {
        await ResetDatabaseAsync();

        CreateProductDto invalidDto = new() { Name = "" }; // Invalid - empty name or missing category

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/products", invalidDto);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Orders_GetByDateRange_InvalidRange_Returns400()
    {
        await ResetDatabaseAsync();

        string start = "2024-12-31T23:59:59Z";
        string end = "2024-01-01T00:00:00Z"; // End before start

        HttpResponseMessage response = await _client.GetAsync($"/api/v1/orders/date-range?startDate={Uri.EscapeDataString(start)}&endDate={Uri.EscapeDataString(end)}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Customers_Update_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();

        UpdateCustomerDto updateDto = new() { FirstName = "X", LastName = "Y", Email = "x@test.com", Gender = "male" };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/v1/customers/999999", updateDto);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Customers_Delete_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.DeleteAsync("/api/v1/customers/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region PATCH Tests

    [Fact]
    public async Task Customers_Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        UpdateCustomerDto patchDto = new UpdateCustomerDto { FirstName = "Patched" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/v1/customers/{customerId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Products_Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int productId = await CreateProductAsync();
        UpdateProductDto patchDto = new UpdateProductDto { Name = "Patched Product" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/v1/products/{productId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Orders_Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        int orderId = await CreateOrderAsync(customerId, addressId);
        UpdateOrderDto patchDto = new()
        { Total = 88.88m };

        HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/v1/orders/{orderId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Articles_Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int articleId = await CreateArticleAsync();
        UpdateArticleDto patchDto = new UpdateArticleDto { Description = "Patched desc" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/v1/articles/{articleId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Labels_Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int labelId = await CreateLabelAsync();
        UpdateLabelDto patchDto = new UpdateLabelDto { Name = "Patched Label" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/v1/labels/{labelId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Colors_Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int colorId = await CreateColorAsync();
        UpdateColorDto patchDto = new UpdateColorDto { Name = "Patched Color" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/v1/colors/{colorId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Sizes_Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int sizeId = await CreateSizeAsync();
        UpdateSizeDto patchDto = new UpdateSizeDto { SizeLabel = "XL" };

        HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/v1/sizes/{sizeId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Stock_Patch_ValidDto_Returns204()
    {
        await ResetDatabaseAsync();
        int stockId = await CreateStockAsync();
        UpdateStockDto patchDto = new UpdateStockDto { Count = 25 };

        HttpResponseMessage response = await _client.PatchAsJsonAsync($"/api/v1/stocks/{stockId}", patchDto);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion

    #region Batch Operations Tests

    [Fact]
    public async Task Customers_CreateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        List<CreateCustomerDto> dtos =
        [
            new() { FirstName = "B1", LastName = "Batch", Email = $"b1-{Guid.NewGuid():N}@test.com", Gender = "male" },
            new() { FirstName = "B2", LastName = "Batch", Email = $"b2-{Guid.NewGuid():N}@test.com", Gender = "female" }
        ];

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/customers/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Customers_UpdateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateCustomerAsync();
        int id2 = await CreateCustomerAsync();
        List<BatchUpdateRequest<UpdateCustomerDto>> updates = new List<BatchUpdateRequest<UpdateCustomerDto>>
        {
            new() { Id = id1, Data = new UpdateCustomerDto { FirstName = "U1" } },
            new() { Id = id2, Data = new UpdateCustomerDto { FirstName = "U2" } }
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/v1/customers/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Customers_DeleteBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateCustomerAsync();
        int id2 = await CreateCustomerAsync();

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/customers/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Products_CreateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        List<CreateProductDto> dtos = new List<CreateProductDto>
        {
            new() { Name = "BatchP1", Category = "Apparel", Gender = "male" },
            new() { Name = "BatchP2", Category = "Footwear", Gender = "female" }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/products/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Products_UpdateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateProductAsync();
        int id2 = await CreateProductAsync();
        List<BatchUpdateRequest<UpdateProductDto>> updates = new List<BatchUpdateRequest<UpdateProductDto>>
        {
            new() { Id = id1, Data = new UpdateProductDto { Name = "UpdatedP1" } },
            new() { Id = id2, Data = new UpdateProductDto { Name = "UpdatedP2" } }
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/v1/products/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Products_DeleteBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateProductAsync();
        int id2 = await CreateProductAsync();

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/products/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Orders_UpdateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        int id1 = await CreateOrderAsync(customerId, addressId);
        int id2 = await CreateOrderAsync(customerId, addressId);
        List<BatchUpdateRequest<UpdateOrderDto>> updates = new List<BatchUpdateRequest<UpdateOrderDto>>
        {
            new() { Id = id1, Data = new UpdateOrderDto { Total = 11.11m } },
            new() { Id = id2, Data = new UpdateOrderDto { Total = 22.22m } }
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/v1/orders/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Orders_DeleteBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int addressId = await CreateAddressAsync(customerId);
        int id1 = await CreateOrderAsync(customerId, addressId);
        int id2 = await CreateOrderAsync(customerId, addressId);

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/orders/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Addresses_CreateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        List<CreateAddressDto> dtos = new List<CreateAddressDto>
        {
            new() { CustomerId = customerId, FirstName = "A1", LastName = "Batch", Address1 = "1 St", City = "C1", Zip = "11111" },
            new() { CustomerId = customerId, FirstName = "A2", LastName = "Batch", Address1 = "2 St", City = "C2", Zip = "22222" }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/addresses/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Addresses_UpdateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int id1 = await CreateAddressAsync(customerId);
        int id2 = await CreateAddressAsync(customerId);
        List<BatchUpdateRequest<UpdateAddressDto>> updates = new List<BatchUpdateRequest<UpdateAddressDto>>
        {
            new() { Id = id1, Data = new UpdateAddressDto { City = "City1" } },
            new() { Id = id2, Data = new UpdateAddressDto { City = "City2" } }
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/v1/addresses/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Addresses_DeleteBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int customerId = await CreateCustomerAsync();
        int id1 = await CreateAddressAsync(customerId);
        int id2 = await CreateAddressAsync(customerId);

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/addresses/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Articles_CreateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int productId = await CreateProductAsync();
        int colorId = await CreateColorAsync();
        int sizeId = await CreateSizeAsync();
        List<CreateArticleDto> dtos = new List<CreateArticleDto>
        {
            new() { ProductId = productId, Ean = $"4{Guid.NewGuid():N}".Substring(0, 13), ColorId = colorId, Size = sizeId, OriginalPrice = 10m, CurrentlyActive = true },
            new() { ProductId = productId, Ean = $"4{Guid.NewGuid():N}".Substring(0, 13), ColorId = colorId, Size = sizeId, OriginalPrice = 20m, CurrentlyActive = true }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/articles/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Articles_UpdateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateArticleAsync();
        int id2 = await CreateArticleAsync();
        List<BatchUpdateRequest<UpdateArticleDto>> updates = new List<BatchUpdateRequest<UpdateArticleDto>>
        {
            new() { Id = id1, Data = new UpdateArticleDto { Description = "Desc1" } },
            new() { Id = id2, Data = new UpdateArticleDto { Description = "Desc2" } }
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/v1/articles/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Articles_DeleteBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateArticleAsync();
        int id2 = await CreateArticleAsync();

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/articles/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Labels_CreateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        List<CreateLabelDto> dtos = new List<CreateLabelDto>
        {
            new() { Name = "BatchL1", SlugName = $"slug1-{Guid.NewGuid():N}" },
            new() { Name = "BatchL2", SlugName = $"slug2-{Guid.NewGuid():N}" }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/labels/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Labels_UpdateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateLabelAsync();
        int id2 = await CreateLabelAsync();
        List<BatchUpdateRequest<UpdateLabelDto>> updates = new List<BatchUpdateRequest<UpdateLabelDto>>
        {
            new() { Id = id1, Data = new UpdateLabelDto { Name = "UpdatedL1" } },
            new() { Id = id2, Data = new UpdateLabelDto { Name = "UpdatedL2" } }
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/v1/labels/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Labels_DeleteBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateLabelAsync();
        int id2 = await CreateLabelAsync();

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/labels/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Colors_CreateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        List<CreateColorDto> dtos = new List<CreateColorDto>
        {
            new() { Name = "BatchC1", Rgb = "#FF0000" },
            new() { Name = "BatchC2", Rgb = "#00FF00" }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/colors/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Colors_UpdateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateColorAsync();
        int id2 = await CreateColorAsync();
        List<BatchUpdateRequest<UpdateColorDto>> updates = new List<BatchUpdateRequest<UpdateColorDto>>
        {
            new() { Id = id1, Data = new UpdateColorDto { Name = "UpdatedC1" } },
            new() { Id = id2, Data = new UpdateColorDto { Name = "UpdatedC2" } }
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/v1/colors/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Colors_DeleteBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateColorAsync();
        int id2 = await CreateColorAsync();

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/colors/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Sizes_CreateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        List<CreateSizeDto> dtos = new List<CreateSizeDto>
        {
            new() { Gender = "male", Category = "Apparel", SizeLabel = "XS" },
            new() { Gender = "female", Category = "Apparel", SizeLabel = "S" }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/sizes/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Sizes_UpdateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateSizeAsync();
        int id2 = await CreateSizeAsync();
        List<BatchUpdateRequest<UpdateSizeDto>> updates = new List<BatchUpdateRequest<UpdateSizeDto>>
        {
            new() { Id = id1, Data = new UpdateSizeDto { SizeLabel = "XXL" } },
            new() { Id = id2, Data = new UpdateSizeDto { SizeLabel = "M" } }
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/v1/sizes/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Sizes_DeleteBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateSizeAsync();
        int id2 = await CreateSizeAsync();

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/sizes/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Stock_CreateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int articleId1 = await CreateArticleAsync();
        int articleId2 = await CreateArticleAsync();
        List<CreateStockDto> dtos = new List<CreateStockDto>
        {
            new() { ArticleId = articleId1, Count = 50 },
            new() { ArticleId = articleId2, Count = 75 }
        };

        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/stocks/batch", dtos);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Stock_UpdateBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateStockAsync();
        int id2 = await CreateStockAsync();
        List<BatchUpdateRequest<UpdateStockDto>> updates = new List<BatchUpdateRequest<UpdateStockDto>>
        {
            new() { Id = id1, Data = new UpdateStockDto { Count = 15 } },
            new() { Id = id2, Data = new UpdateStockDto { Count = 30 } }
        };

        HttpResponseMessage response = await _client.PutAsJsonAsync("/api/v1/stocks/batch", updates);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Stock_DeleteBatch_Returns200()
    {
        await ResetDatabaseAsync();
        int id1 = await CreateStockAsync();
        int id2 = await CreateStockAsync();

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/stocks/batch") { Content = JsonContent.Create(new[] { id1, id2 }) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Cache Management Tests

    [Fact]
    public async Task CacheManagement_ClearByKeys_Returns200()
    {
        await ResetDatabaseAsync();

        var request = new { Keys = new[] { "test-key-1", "test-key-2" } };

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/cache-management/keys") { Content = JsonContent.Create(request) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CacheManagement_ClearByTag_Returns200()
    {
        await ResetDatabaseAsync();

        var request = new { Tag = "test-tag" };

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/cache-management/tag") { Content = JsonContent.Create(request) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CacheManagement_ClearByTags_Returns200()
    {
        await ResetDatabaseAsync();

        var request = new { Tags = new[] { "tag1", "tag2" } };

        HttpResponseMessage response = await _client.SendAsync(new HttpRequestMessage(HttpMethod.Delete, "/api/v1/cache-management/tags") { Content = JsonContent.Create(request) });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task CacheManagement_ClearByKey_Returns200()
    {
        await ResetDatabaseAsync();

        HttpResponseMessage response = await _client.DeleteAsync("/api/v1/cache-management/key/test-key-123");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    #endregion

    #region Additional Coverage Tests

    [Fact]
    public async Task Labels_GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await _client.GetAsync("/api/v1/labels/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Colors_GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await _client.GetAsync("/api/v1/colors/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Sizes_GetById_InvalidId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await _client.GetAsync("/api/v1/sizes/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Products_GetByCategory_ValidCategoryNoProducts_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await _client.GetAsync("/api/v1/products/category/Luggage");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Customers_GetByEmail_InvalidEmail_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await _client.GetAsync("/api/v1/customers/email/nonexistent@test.com");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Articles_GetByEan_InvalidEan_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await _client.GetAsync("/api/v1/articles/ean/0000000000000");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Stock_GetByArticleId_InvalidArticleId_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await _client.GetAsync("/api/v1/stocks/article/999999");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Labels_GetBySlugName_InvalidSlug_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await _client.GetAsync("/api/v1/labels/slug/nonexistent-slug-12345");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Colors_GetByName_InvalidName_Returns404()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await _client.GetAsync("/api/v1/colors/name/NonexistentColorName123");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Orders_GetAll_NonPaginated_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await _client.GetAsync("/api/v1/orders");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        Response<object>? result = await response.Content.ReadFromJsonAsync<Response<object>>();
        result!.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Customers_Create_EmptyBatch_Returns200()
    {
        await ResetDatabaseAsync();
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/customers/batch", Array.Empty<CreateCustomerDto>());
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Created);
    }

    #endregion

    #region Helpers

    private async Task<int> CreateCustomerAsync()
    {
        CreateCustomerDto dto = new()
        {
            FirstName = "Order",
            LastName = "Test",
            Email = $"order-{Guid.NewGuid():N}@test.example.com",
            Gender = "male"
        };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/customers", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<CustomerDto>? result = await response.Content.ReadFromJsonAsync<Response<CustomerDto>>();
        return result!.Data!.Id;
    }

    private async Task<int> CreateAddressAsync(int customerId)
    {
        CreateAddressDto dto = new()
        {
            CustomerId = customerId,
            FirstName = "Ship",
            LastName = "User",
            Address1 = "456 Ship St",
            City = "ShipCity",
            Zip = "67890"
        };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/addresses", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<AddressDto>? result = await response.Content.ReadFromJsonAsync<Response<AddressDto>>();
        return result!.Data!.Id;
    }

    private async Task<int> CreateProductAsync()
    {
        CreateProductDto dto = new() { Name = $"Product-{Guid.NewGuid():N}", Category = "Apparel", Gender = "male" };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/products", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ProductDto>? result = await response.Content.ReadFromJsonAsync<Response<ProductDto>>();
        return result!.Data!.Id;
    }

    private async Task<int> CreateOrderAsync(int customerId, int addressId)
    {
        CreateOrderDto dto = new()
        {
            CustomerId = customerId,
            ShippingAddressId = addressId,
            ShippingCost = 9.99m
        };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/orders", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<OrderDto>? result = await response.Content.ReadFromJsonAsync<Response<OrderDto>>();
        return result!.Data!.Id;
    }

    private async Task<int> CreateLabelAsync(string? slugName = null)
    {
        string slug = slugName ?? $"slug-{Guid.NewGuid():N}";
        CreateLabelDto dto = new() { Name = $"Label-{slug}", SlugName = slug };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/labels", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<LabelDto>? result = await response.Content.ReadFromJsonAsync<Response<LabelDto>>();
        return result!.Data!.Id;
    }

    private async Task<int> CreateColorAsync(string? name = null)
    {
        CreateColorDto dto = new() { Name = name ?? $"Color-{Guid.NewGuid():N}", Rgb = "#FF0000" };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/colors", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ColorDto>? result = await response.Content.ReadFromJsonAsync<Response<ColorDto>>();
        return result!.Data!.Id;
    }

    private async Task<int> CreateSizeAsync()
    {
        CreateSizeDto dto = new() { Gender = "male", Category = "Apparel", SizeLabel = $"S{Guid.NewGuid():N}".Substring(0, 8) };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/sizes", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<SizeDto>? result = await response.Content.ReadFromJsonAsync<Response<SizeDto>>();
        return result!.Data!.Id;
    }

    private async Task<int> CreateArticleAsync(int? productId = null, string? ean = null)
    {
        int pid = productId ?? await CreateProductAsync();
        int colorId = await CreateColorAsync();
        int sizeId = await CreateSizeAsync();
        string articleEan = ean ?? $"4{Guid.NewGuid():N}".Substring(0, 13);
        CreateArticleDto dto = new()
        {
            ProductId = pid,
            Ean = articleEan,
            ColorId = colorId,
            Size = sizeId,
            OriginalPrice = 49.99m,
            CurrentlyActive = true
        };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/articles", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<ArticleDto>? result = await response.Content.ReadFromJsonAsync<Response<ArticleDto>>();
        return result!.Data!.Id;
    }

    private async Task<int> CreateStockAsync(int? articleId = null)
    {
        int aid = articleId ?? await CreateArticleAsync();
        CreateStockDto dto = new() { ArticleId = aid, Count = 100 };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/api/v1/stocks", dto);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        Response<StockDto>? result = await response.Content.ReadFromJsonAsync<Response<StockDto>>();
        return result!.Data!.Id;
    }

    #endregion
}
