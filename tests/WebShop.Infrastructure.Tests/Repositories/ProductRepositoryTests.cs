using FluentAssertions;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.Infrastructure.Tests.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Repositories;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class ProductRepositoryTests
{
    private readonly Helpers.TestDatabaseFixture _fixture;
    private readonly ProductRepository _productRepository;
    private readonly LabelRepository _labelRepository;

    public ProductRepositoryTests(Helpers.TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        _productRepository = new ProductRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _labelRepository = new LabelRepository(_fixture.ConnectionFactory, null, loggerFactory);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsProduct()
    {
        await _fixture.ResetDatabaseAsync();

        Label label = new() { Name = "Test Brand", SlugName = "test-brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);

        Product product = new() { Name = "Test Product", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _productRepository.AddAsync(product);

        Product? result = await _productRepository.GetByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.Name.Should().Be("Test Product");
        result.Category.Should().Be("Apparel");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        Product? result = await _productRepository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveProducts()
    {
        await _fixture.ResetDatabaseAsync();

        Label label = new() { Name = "Brand", SlugName = "brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);

        await _productRepository.AddAsync(new Product { Name = "Product 1", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 });
        await _productRepository.AddAsync(new Product { Name = "Product 2", LabelId = label.Id, Category = "Footwear", Gender = "female", CurrentlyActive = false, CreatedBy = 1, UpdatedBy = 1 });

        IReadOnlyList<Product> result = await _productRepository.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_ValidParams_ReturnsPagedResult()
    {
        await _fixture.ResetDatabaseAsync();

        Label label = new() { Name = "Brand", SlugName = "brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);

        for (int i = 0; i < 5; i++)
        {
            await _productRepository.AddAsync(new Product { Name = $"Product {i}", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 });
        }

        var (items, totalCount) = await _productRepository.GetPagedAsync(1, 10);

        items.Should().NotBeNull();
        items.Should().HaveCount(5);
        totalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetPagedAsync_EmptyPage_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();

        var (items, totalCount) = await _productRepository.GetPagedAsync(1, 10);

        items.Should().BeEmpty();
        totalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetByCategoryAsync_ValidCategory_ReturnsProducts()
    {
        await _fixture.ResetDatabaseAsync();

        Label label = new() { Name = "Brand", SlugName = "brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);

        await _productRepository.AddAsync(new Product { Name = "Shirt", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 });

        List<Product> result = await _productRepository.GetByCategoryAsync("Apparel");

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Category.Should().Be("Apparel");
    }

    [Fact]
    public async Task GetActiveProductsAsync_ReturnsActiveProducts()
    {
        await _fixture.ResetDatabaseAsync();

        Label label = new() { Name = "Brand", SlugName = "brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);

        await _productRepository.AddAsync(new Product { Name = "Active Product", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 });

        List<Product> result = await _productRepository.GetActiveProductsAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindByIdsAsync_ValidIds_ReturnsProducts()
    {
        await _fixture.ResetDatabaseAsync();

        Label label = new() { Name = "Brand", SlugName = "brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);

        Product product = new() { Name = "Product 1", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _productRepository.AddAsync(product);

        IReadOnlyList<Product> result = await _productRepository.FindByIdsAsync(new[] { product.Id, 999 });

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task FindByIdsAsync_EmptyList_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();

        IReadOnlyList<Product> result = await _productRepository.FindByIdsAsync(Array.Empty<int>());

        result.Should().BeEmpty();
    }
}
