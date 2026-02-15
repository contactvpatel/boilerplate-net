using FluentAssertions;
using Microsoft.Extensions.Logging;
using WebShop.Core.Entities;
using WebShop.Infrastructure.Repositories;
using WebShop.Infrastructure.Tests.Helpers;
using Xunit;

namespace WebShop.Infrastructure.Tests.Repositories;

[Collection("IntegrationDatabase")]
[Trait("Category", TestCategories.Integration)]
public class OrderPositionRepositoryTests
{
    private readonly Helpers.TestDatabaseFixture _fixture;
    private readonly OrderPositionRepository _orderPositionRepository;
    private readonly OrderRepository _orderRepository;
    private readonly ArticleRepository _articleRepository;
    private readonly CustomerRepository _customerRepository;
    private readonly AddressRepository _addressRepository;
    private readonly ProductRepository _productRepository;
    private readonly LabelRepository _labelRepository;
    private readonly ColorRepository _colorRepository;

    public OrderPositionRepositoryTests(Helpers.TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        ILoggerFactory loggerFactory = LoggerFactory.Create(b => b.SetMinimumLevel(LogLevel.Warning));
        _orderPositionRepository = new OrderPositionRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _orderRepository = new OrderRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _articleRepository = new ArticleRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _customerRepository = new CustomerRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _addressRepository = new AddressRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _productRepository = new ProductRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _labelRepository = new LabelRepository(_fixture.ConnectionFactory, null, loggerFactory);
        _colorRepository = new ColorRepository(_fixture.ConnectionFactory, null, loggerFactory);
    }

    private async Task<(Order Order, Article Article)> CreateOrderWithArticleAsync()
    {
        Customer customer = new() { FirstName = "John", LastName = "Doe", Gender = "male", Email = "john@example.com", CreatedBy = 1, UpdatedBy = 1 };
        await _customerRepository.AddAsync(customer);
        Address address = new() { CustomerId = customer.Id, FirstName = "John", LastName = "Doe", Address1 = "123 Main St", City = "New York", Zip = "10001", CreatedBy = 1, UpdatedBy = 1 };
        await _addressRepository.AddAsync(address);
        Order order = new() { CustomerId = customer.Id, OrderTimestamp = DateTime.UtcNow, ShippingAddressId = address.Id, Total = 99.99m, ShippingCost = 5.00m, CreatedBy = 1, UpdatedBy = 1 };
        await _orderRepository.AddAsync(order);

        Label label = new() { Name = "Brand", SlugName = "brand", CreatedBy = 1, UpdatedBy = 1 };
        await _labelRepository.AddAsync(label);
        Product product = new() { Name = "Product", LabelId = label.Id, Category = "Apparel", Gender = "male", CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _productRepository.AddAsync(product);
        Color color = new() { Name = "Red", Rgb = "#FF0000", CreatedBy = 1, UpdatedBy = 1 };
        await _colorRepository.AddAsync(color);
        Article article = new() { ProductId = product.Id, Ean = "1234567890123", ColorId = color.Id, Size = 42, Description = "Article", OriginalPrice = 49.99m, ReducedPrice = 39.99m, TaxRate = 19.0m, DiscountInPercent = 10, CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _articleRepository.AddAsync(article);

        return (order, article);
    }

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsOrderPosition()
    {
        await _fixture.ResetDatabaseAsync();

        (Order? order, Article? article) = await CreateOrderWithArticleAsync();
        OrderPosition position = new() { OrderId = order.Id, ArticleId = article.Id, Amount = 2, Price = 29.99m, CreatedBy = 1, UpdatedBy = 1 };
        await _orderPositionRepository.AddAsync(position);

        OrderPosition? result = await _orderPositionRepository.GetByIdAsync(position.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(position.Id);
        result.OrderId.Should().Be(order.Id);
        result.ArticleId.Should().Be(article.Id);
        result.Amount.Should().Be(2);
        result.Price.Should().Be(29.99m);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        await _fixture.ResetDatabaseAsync();

        OrderPosition? result = await _orderPositionRepository.GetByIdAsync(999);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_ReturnsAllActiveOrderPositions()
    {
        await _fixture.ResetDatabaseAsync();

        (Order? order, Article? article) = await CreateOrderWithArticleAsync();
        Article article2 = new() { ProductId = article.ProductId, Ean = "1234567890124", ColorId = article.ColorId, Size = 44, Description = "Article 2", OriginalPrice = 49.99m, ReducedPrice = 39.99m, TaxRate = 19.0m, DiscountInPercent = 10, CurrentlyActive = true, CreatedBy = 1, UpdatedBy = 1 };
        await _articleRepository.AddAsync(article2);

        await _orderPositionRepository.AddAsync(new OrderPosition { OrderId = order.Id, ArticleId = article.Id, Amount = 2, Price = 29.99m, CreatedBy = 1, UpdatedBy = 1 });
        await _orderPositionRepository.AddAsync(new OrderPosition { OrderId = order.Id, ArticleId = article2.Id, Amount = 1, Price = 49.99m, CreatedBy = 1, UpdatedBy = 1 });

        IReadOnlyList<OrderPosition> result = await _orderPositionRepository.GetAllAsync();

        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPagedAsync_ValidParams_ReturnsPagedResult()
    {
        await _fixture.ResetDatabaseAsync();

        (Order? order, Article? article) = await CreateOrderWithArticleAsync();
        for (int i = 0; i < 20; i++)
        {
            await _orderPositionRepository.AddAsync(new OrderPosition { OrderId = order.Id, ArticleId = article.Id, Amount = 1, Price = 29.99m, CreatedBy = 1, UpdatedBy = 1 });
        }

        (IReadOnlyList<OrderPosition>? items, int totalCount) = await _orderPositionRepository.GetPagedAsync(1, 10);

        items.Should().NotBeNull();
        items.Should().HaveCount(10);
        totalCount.Should().Be(20);
    }

    [Fact]
    public async Task GetByOrderIdAsync_ValidOrderId_ReturnsPositions()
    {
        await _fixture.ResetDatabaseAsync();

        (Order? order, Article? article) = await CreateOrderWithArticleAsync();
        await _orderPositionRepository.AddAsync(new OrderPosition { OrderId = order.Id, ArticleId = article.Id, Amount = 2, Price = 29.99m, CreatedBy = 1, UpdatedBy = 1 });

        List<OrderPosition> result = await _orderPositionRepository.GetByOrderIdAsync(order.Id);

        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].OrderId.Should().Be(order.Id);
    }

    [Fact]
    public async Task FindByIdsAsync_EmptyList_ReturnsEmpty()
    {
        await _fixture.ResetDatabaseAsync();

        IReadOnlyList<OrderPosition> result = await _orderPositionRepository.FindByIdsAsync(Array.Empty<int>());

        result.Should().BeEmpty();
    }
}
