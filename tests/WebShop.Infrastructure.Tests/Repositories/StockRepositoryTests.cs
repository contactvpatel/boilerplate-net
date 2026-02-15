using FluentAssertions;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.Infrastructure.Tests.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Repositories;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class StockRepositoryTests
{
    private readonly Helpers.TestDatabaseFixture _fixture;
    private readonly StockRepository _stockRepository;
    private readonly ArticleRepository _articleRepository;
    private readonly ProductRepository _productRepository;
    private readonly LabelRepository _labelRepository;
    private readonly ColorRepository _colorRepository;

    public StockRepositoryTests(Helpers.TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        _stockRepository = new StockRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _articleRepository = new ArticleRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _productRepository = new ProductRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _labelRepository = new LabelRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _colorRepository = new ColorRepository(_fixture.ConnectionFactory, null, loggerFactory);
    }

    private async Task<Article> CreateArticleAsync()
    {
        Label label = new() { Name = "Brand", SlugName = "brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);
        Product product = new() { Name = "Product", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _productRepository.AddAsync(product);
        Color color = new() { Name = "Red", Rgb = "#FF0000", CreatedBy = 1, UpdatedBy = 1 };
        await _colorRepository.AddAsync(color);
        Article article = new() { ProductId = product.Id, Ean = "1234567890123", ColorId = color.Id, Size = 42, Description = "Article", OriginalPrice = 49.99m, ReducedPrice = 39.99m, TaxRate = 19.0m, DiscountInPercent = 10, CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _articleRepository.AddAsync(article);
        return article;
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsStock()
    {
        await _fixture.ResetDatabaseAsync();

        Article article = await CreateArticleAsync();
        Stock stock = new() { ArticleId = article.Id, Count = 50, CreatedBy = 1, UpdatedBy = 1 };
        await _stockRepository.AddAsync(stock);

        Stock? result = await _stockRepository.GetByIdAsync(stock.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(stock.Id);
        result.ArticleId.Should().Be(article.Id);
        result.Count.Should().Be(50);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        Stock? result = await _stockRepository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveStock()
    {
        await _fixture.ResetDatabaseAsync();

        Article article1 = await CreateArticleAsync();
        Article article2 = await CreateArticleAsync();
        await _stockRepository.AddAsync(new Stock { ArticleId = article1.Id, Count = 50, CreatedBy = 1, UpdatedBy = 1 });
        await _stockRepository.AddAsync(new Stock { ArticleId = article2.Id, Count = 25, CreatedBy = 1, UpdatedBy = 1 });

        IReadOnlyList<Stock> result = await _stockRepository.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_ValidParams_ReturnsPagedResult()
    {
        await _fixture.ResetDatabaseAsync();

        Article article = await CreateArticleAsync();
        for (int i = 0; i < 8; i++)
        {
            await _stockRepository.AddAsync(new Stock { ArticleId = article.Id, Count = 50 + i, CreatedBy = 1, UpdatedBy = 1 });
        }

        (IReadOnlyList<Stock>? items, int totalCount) = await _stockRepository.GetPagedAsync(1, 10);

        items.Should().NotBeNull();
        items.Should().HaveCount(8);
        totalCount.Should().Be(8);
    }

    [Fact]
    public async Task GetByArticleIdAsync_ValidArticleId_ReturnsStock()
    {
        await _fixture.ResetDatabaseAsync();

        Article article = await CreateArticleAsync();
        Stock stock = new() { ArticleId = article.Id, Count = 50, CreatedBy = 1, UpdatedBy = 1 };
        await _stockRepository.AddAsync(stock);

        Stock? result = await _stockRepository.GetByArticleIdAsync(article.Id);

        result.Should().NotBeNull();
        result!.ArticleId.Should().Be(article.Id);
    }

    [Fact]
    public async Task GetByArticleIdAsync_InvalidArticleId_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        Stock? result = await _stockRepository.GetByArticleIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLowStockAsync_ReturnsLowStockItems()
    {
        await _fixture.ResetDatabaseAsync();

        Article article = await CreateArticleAsync();
        await _stockRepository.AddAsync(new Stock { ArticleId = article.Id, Count = 5, CreatedBy = 1, UpdatedBy = 1 });

        List<Stock> result = await _stockRepository.GetLowStockAsync(10);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].Count.Should().Be(5);
    }

    [Fact]
    public async Task FindByIdsAsync_EmptyList_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();

        IReadOnlyList<Stock> result = await _stockRepository.FindByIdsAsync(Array.Empty<int>());

        result.Should().BeEmpty();
    }
}
