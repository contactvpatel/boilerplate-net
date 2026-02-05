using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Business.DTOs;
using WebShop.Business.Services;
using WebShop.Core.Entities;
using WebShop.Core.Interfaces;
using Xunit;

namespace WebShop.Business.Tests.Services;

/// <summary>
/// Unit tests for ProductService.
/// </summary>
[Trait("Category", "Unit")]
public class ProductServiceTests
{
    private readonly Mock<IProductRepository> _mockRepository;
    private readonly Mock<ILogger<ProductService>> _mockLogger;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        _mockRepository = new Mock<IProductRepository>();
        _mockLogger = new Mock<ILogger<ProductService>>();
        _service = new ProductService(_mockRepository.Object, _mockLogger.Object);
    }

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_ValidId_ReturnsProductDto()
    {
        // Arrange
        const int productId = 1;
        Product product = new Product
        {
            Id = productId,
            Name = "Test Product",
            Category = "Electronics",
            LabelId = 1
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        // Act
        ProductDto? result = await _service.GetByIdAsync(productId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(productId);
        result.Name.Should().Be("Test Product");
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int productId = 999;
        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        ProductDto? result = await _service.GetByIdAsync(productId);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_NegativeId_ReturnsNull()
    {
        // Arrange
        const int productId = -1;
        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        ProductDto? result = await _service.GetByIdAsync(productId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ZeroId_ReturnsNull()
    {
        // Arrange
        const int productId = 0;
        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        ProductDto? result = await _service.GetByIdAsync(productId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region GetAllAsync Tests

    [Fact]
    public async Task GetAllAsync_ReturnsAllProducts()
    {
        // Arrange
        List<Product> products = new List<Product>
        {
            new() { Id = 1, Name = "Product 1", Category = "Electronics" },
            new() { Id = 2, Name = "Product 2", Category = "Clothing" }
        };

        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        // Act
        IReadOnlyList<ProductDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[1].Id.Should().Be(2);
        _mockRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAllAsync_NoProducts_ReturnsEmptyList()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Product>());

        // Act
        IReadOnlyList<ProductDto> result = await _service.GetAllAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_ValidDto_CreatesProduct()
    {
        // Arrange
        CreateProductDto createDto = new CreateProductDto
        {
            Name = "New Product",
            Category = "Electronics",
            LabelId = 1
        };

        Product product = new Product
        {
            Id = 1,
            Name = "New Product",
            Category = "Electronics",
            LabelId = 1
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken cancellationToken) => p);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        ProductDto result = await _service.CreateAsync(createDto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("New Product");
        result.Category.Should().Be("Electronics");
        _mockRepository.Verify(r => r.AddAsync(It.Is<Product>(p => p.Name == "New Product"), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        CreateProductDto? createDto = null;

        // Act
        Func<Task> act = async () => await _service.CreateAsync(createDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_ValidId_UpdatesProduct()
    {
        // Arrange
        const int productId = 1;
        UpdateProductDto updateDto = new UpdateProductDto
        {
            Name = "Updated Product",
            Category = "Updated Category"
        };

        Product existingProduct = new Product
        {
            Id = productId,
            Name = "Original Product",
            Category = "Original Category"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        ProductDto? result = await _service.UpdateAsync(productId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Updated Product");
        result.Category.Should().Be("Updated Category");
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int productId = 999;
        UpdateProductDto updateDto = new UpdateProductDto
        {
            Name = "Updated Product",
            Category = "Updated Category"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        ProductDto? result = await _service.UpdateAsync(productId, updateDto);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        const int productId = 1;
        UpdateProductDto? updateDto = null;

        // Act
        Func<Task> act = async () => await _service.UpdateAsync(productId, updateDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region PatchAsync Tests

    [Fact]
    public async Task PatchAsync_ValidId_WithChanges_PatchesProduct()
    {
        // Arrange
        const int productId = 1;
        UpdateProductDto patchDto = new UpdateProductDto
        {
            Name = "Patched Product"
        };

        Product existingProduct = new Product
        {
            Id = productId,
            Name = "Original Product",
            Category = "Original Category"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        ProductDto? result = await _service.PatchAsync(productId, patchDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Patched Product");
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PatchAsync_ValidId_NoChanges_ReturnsProductWithoutSaving()
    {
        // Arrange
        const int productId = 1;
        UpdateProductDto patchDto = new UpdateProductDto
        {
            Name = "Original Product" // Same as existing
        };

        Product existingProduct = new Product
        {
            Id = productId,
            Name = "Original Product",
            Category = "Original Category"
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        ProductDto? result = await _service.PatchAsync(productId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PatchAsync_InvalidId_ReturnsNull()
    {
        // Arrange
        const int productId = 999;
        UpdateProductDto patchDto = new UpdateProductDto { Name = "Patched Product" };

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        // Act
        ProductDto? result = await _service.PatchAsync(productId, patchDto);

        // Assert
        result.Should().BeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PatchAsync_NullDto_ThrowsArgumentNullException()
    {
        // Arrange
        const int productId = 1;
        UpdateProductDto? patchDto = null;

        // Act
        Func<Task> act = async () => await _service.PatchAsync(productId, patchDto!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_ValidId_DeletesProduct()
    {
        // Arrange
        const int productId = 1;
        Product product = new Product
        {
            Id = productId,
            Name = "Product to Delete",
            Category = "Electronics"
        };

        _mockRepository
            .Setup(r => r.ExistsAsync(productId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        bool result = await _service.DeleteAsync(productId);

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.ExistsAsync(productId, true, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_InvalidId_ReturnsFalse()
    {
        // Arrange
        const int productId = 999;

        _mockRepository
            .Setup(r => r.ExistsAsync(productId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        bool result = await _service.DeleteAsync(productId);

        // Assert
        result.Should().BeFalse();
        _mockRepository.Verify(r => r.ExistsAsync(productId, true, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_AlreadyDeleted_ReturnsTrue()
    {
        // Arrange
        const int productId = 1;

        _mockRepository
            .Setup(r => r.ExistsAsync(productId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null); // Already soft-deleted

        // Act
        bool result = await _service.DeleteAsync(productId);

        // Assert
        result.Should().BeTrue(); // Idempotent - returns true even if already deleted
        _mockRepository.Verify(r => r.ExistsAsync(productId, true, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetByCategoryAsync Tests

    [Fact]
    public async Task GetByCategoryAsync_ValidCategory_ReturnsProducts()
    {
        // Arrange
        const string category = "Electronics";
        List<Product> products = new List<Product>
        {
            new() { Id = 1, Name = "Product 1", Category = category },
            new() { Id = 2, Name = "Product 2", Category = category }
        };

        _mockRepository
            .Setup(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        // Act
        IReadOnlyList<ProductDto> result = await _service.GetByCategoryAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockRepository.Verify(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByCategoryAsync_EmptyCategory_ReturnsEmptyList()
    {
        // Arrange
        const string category = "";

        _mockRepository
            .Setup(r => r.GetByCategoryAsync(category, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Product>());

        // Act
        IReadOnlyList<ProductDto> result = await _service.GetByCategoryAsync(category);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetActiveProductsAsync Tests

    [Fact]
    public async Task GetActiveProductsAsync_ReturnsActiveProducts()
    {
        // Arrange
        List<Product> products = new List<Product>
        {
            new() { Id = 1, Name = "Active Product 1", CurrentlyActive = true },
            new() { Id = 2, Name = "Active Product 2", CurrentlyActive = true }
        };

        _mockRepository
            .Setup(r => r.GetActiveProductsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        // Act
        IReadOnlyList<ProductDto> result = await _service.GetActiveProductsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockRepository.Verify(r => r.GetActiveProductsAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateBatchAsync Tests

    [Fact]
    public async Task CreateBatchAsync_ValidDtos_CreatesProducts()
    {
        // Arrange
        List<CreateProductDto> createDtos = new List<CreateProductDto>
        {
            new() { Name = "Product 1", Category = "Electronics" },
            new() { Name = "Product 2", Category = "Clothing" }
        };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product p, CancellationToken cancellationToken) => p);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<ProductDto> result = await _service.CreateBatchAsync(createDtos);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBatchAsync_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        List<CreateProductDto> createDtos = new List<CreateProductDto>();

        // Act
        IReadOnlyList<ProductDto> result = await _service.CreateBatchAsync(createDtos);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateBatchAsync_NullDtos_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<CreateProductDto>? createDtos = null;

        // Act
        Func<Task> act = async () => await _service.CreateBatchAsync(createDtos!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region UpdateBatchAsync Tests

    [Fact]
    public async Task UpdateBatchAsync_ValidUpdates_UpdatesProducts()
    {
        // Arrange
        List<(int Id, UpdateProductDto UpdateDto)> updates = new List<(int, UpdateProductDto)>
        {
            (1, new UpdateProductDto { Name = "Updated Product 1" }),
            (2, new UpdateProductDto { Name = "Updated Product 2" })
        };

        List<Product> products = new List<Product>
        {
            new() { Id = 1, Name = "Original Product 1" },
            new() { Id = 2, Name = "Original Product 2" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<ProductDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockRepository.Verify(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBatchAsync_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        List<(int Id, UpdateProductDto UpdateDto)> updates = new List<(int, UpdateProductDto)>();

        // Act
        IReadOnlyList<ProductDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateBatchAsync_NullUpdates_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<(int Id, UpdateProductDto UpdateDto)>? updates = null;

        // Act
        Func<Task> act = async () => await _service.UpdateBatchAsync(updates!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UpdateBatchAsync_SomeProductsNotFound_SkipsMissingProducts()
    {
        // Arrange
        List<(int Id, UpdateProductDto UpdateDto)> updates = new List<(int, UpdateProductDto)>
        {
            (1, new UpdateProductDto { Name = "Updated Product 1" }),
            (2, new UpdateProductDto { Name = "Updated Product 2" }),
            (999, new UpdateProductDto { Name = "Missing Product" }) // Not found
        };

        List<Product> products = new List<Product>
        {
            new() { Id = 1, Name = "Original Product 1" },
            new() { Id = 2, Name = "Original Product 2" }
            // Product 999 is missing
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<ProductDto> result = await _service.UpdateBatchAsync(updates);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2); // Only 2 products updated, 1 skipped
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    #endregion

    #region DeleteBatchAsync Tests

    [Fact]
    public async Task DeleteBatchAsync_ValidIds_DeletesProducts()
    {
        // Arrange
        List<int> ids = new List<int> { 1, 2 };
        List<Product> products = new List<Product>
        {
            new() { Id = 1, Name = "Product 1" },
            new() { Id = 2, Name = "Product 2" }
        };

        _mockRepository
            .Setup(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(products);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        // Act
        IReadOnlyList<int> result = await _service.DeleteBatchAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().Contain(1);
        result.Should().Contain(2);
        _mockRepository.Verify(r => r.FindAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Product, bool>>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteBatchAsync_EmptyList_ReturnsEmptyList()
    {
        // Arrange
        List<int> ids = new List<int>();

        // Act
        IReadOnlyList<int> result = await _service.DeleteBatchAsync(ids);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteBatchAsync_NullIds_ThrowsArgumentNullException()
    {
        // Arrange
        IReadOnlyList<int>? ids = null;

        // Act
        Func<Task> act = async () => await _service.DeleteBatchAsync(ids!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task CreateAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        CreateProductDto createDto = new CreateProductDto { Name = "New Product", Category = "Electronics" };

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(new Product()));

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.CreateAsync(createDto);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task PatchAsync_NoChanges_DoesNotCallUpdate()
    {
        // Arrange
        const int productId = 1;
        Product existingProduct = new Product
        {
            Id = productId,
            Name = "Product",
            Category = "Electronics",
            LabelId = 1,
            Gender = "Unisex",
            CurrentlyActive = true
        };

        UpdateProductDto patchDto = new UpdateProductDto
        {
            Name = "Product", // Same value
            Category = "Electronics", // Same value
            LabelId = 1 // Same value
        };

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        // Act
        ProductDto? result = await _service.PatchAsync(productId, patchDto);

        // Assert
        result.Should().NotBeNull();
        _mockRepository.Verify(r => r.UpdateAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_RepositoryThrowsException_PropagatesException()
    {
        // Arrange
        const int productId = 1;
        Product existingProduct = new Product { Id = productId, Name = "Product", Category = "Electronics" };

        _mockRepository
            .Setup(r => r.ExistsAsync(productId, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingProduct);

        _mockRepository
            .Setup(r => r.DeleteAsync(It.IsAny<Product>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        // Act & Assert
        Func<Task> act = async () => await _service.DeleteAsync(productId);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    #endregion
}
