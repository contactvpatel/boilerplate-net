using FluentAssertions;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.Infrastructure.Tests.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Repositories;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class ArticleRepositoryTests
{
    private readonly Helpers.TestDatabaseFixture _fixture;
    private readonly ArticleRepository _articleRepository;
    private readonly ProductRepository _productRepository;
    private readonly LabelRepository _labelRepository;
    private readonly ColorRepository _colorRepository;

    public ArticleRepositoryTests(Helpers.TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        _articleRepository = new ArticleRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _productRepository = new ProductRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _labelRepository = new LabelRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _colorRepository = new ColorRepository(_fixture.ConnectionFactory, null, loggerFactory);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsArticle()
    {
        await _fixture.ResetDatabaseAsync();

        Label label = new() { Name = "Brand", SlugName = "brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);
        Product product = new() { Name = "Product", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _productRepository.AddAsync(product);
        Color color = new() { Name = "Red", Rgb = "#FF0000", CreatedBy = 1, UpdatedBy = 1 };
        await _colorRepository.AddAsync(color);

        Article article = new()
        {
            ProductId = product.Id,
            Ean = "1234567890123",
            ColorId = color.Id,
            Size = 42,
            Description = "Test Article",
            OriginalPrice = 49.99m,
            ReducedPrice = 39.99m,
            TaxRate = 19.0m,
            DiscountInPercent = 10,
            CurrentlyActive = true,
            CreatedBy = 1,
            UpdatedBy = 1
        };
        await _articleRepository.AddAsync(article);

        Article? result = await _articleRepository.GetByIdAsync(article.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(article.Id);
        result.ProductId.Should().Be(product.Id);
        result.Ean.Should().Be("1234567890123");
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        Article? result = await _articleRepository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveArticles()
    {
        await _fixture.ResetDatabaseAsync();

        Label label = new() { Name = "Brand", SlugName = "brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);
        Product product = new() { Name = "Product", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _productRepository.AddAsync(product);
        Color color = new() { Name = "Red", Rgb = "#FF0000", CreatedBy = 1, UpdatedBy = 1 };
        await _colorRepository.AddAsync(color);

        Article article = new() { ProductId = product.Id, Ean = "1234567890123", ColorId = color.Id, Size = 42, Description = "Article 1", OriginalPrice = 49.99m, ReducedPrice = 39.99m, TaxRate = 19.0m, DiscountInPercent = 10, CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _articleRepository.AddAsync(article);

        IReadOnlyList<Article> result = await _articleRepository.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetPagedAsync_ValidParams_ReturnsPagedResult()
    {
        await _fixture.ResetDatabaseAsync();

        Label label = new() { Name = "Brand", SlugName = "brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);
        Product product = new() { Name = "Product", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _productRepository.AddAsync(product);
        Color color = new() { Name = "Red", Rgb = "#FF0000", CreatedBy = 1, UpdatedBy = 1 };
        await _colorRepository.AddAsync(color);

        for (int i = 0; i < 15; i++)
        {
            await _articleRepository.AddAsync(new Article { ProductId = product.Id, Ean = $"1234567890{i:D3}", ColorId = color.Id, Size = 42, Description = $"Article {i}", OriginalPrice = 49.99m, ReducedPrice = 39.99m, TaxRate = 19.0m, DiscountInPercent = 10, CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 });
        }

        (IReadOnlyList<Article>? items, int totalCount) = await _articleRepository.GetPagedAsync(1, 10);

        items.Should().NotBeNull();
        items.Should().HaveCount(10);
        totalCount.Should().Be(15);
    }

    [Fact]
    public async Task GetByProductIdAsync_ValidProductId_ReturnsArticles()
    {
        await _fixture.ResetDatabaseAsync();

        Label label = new() { Name = "Brand", SlugName = "brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);
        Product product = new() { Name = "Product", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _productRepository.AddAsync(product);
        Color color = new() { Name = "Red", Rgb = "#FF0000", CreatedBy = 1, UpdatedBy = 1 };
        await _colorRepository.AddAsync(color);

        Article article = new() { ProductId = product.Id, Ean = "1234567890123", ColorId = color.Id, Size = 42, Description = "Article 1", OriginalPrice = 49.99m, ReducedPrice = 39.99m, TaxRate = 19.0m, DiscountInPercent = 10, CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _articleRepository.AddAsync(article);

        List<Article> result = await _articleRepository.GetByProductIdAsync(product.Id);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].ProductId.Should().Be(product.Id);
    }

    [Fact]
    public async Task FindByIdsAsync_EmptyList_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();

        IReadOnlyList<Article> result = await _articleRepository.FindByIdsAsync(Array.Empty<int>());

        result.Should().BeEmpty();
    }
}
