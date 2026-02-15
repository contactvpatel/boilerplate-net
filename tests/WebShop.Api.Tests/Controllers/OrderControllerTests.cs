using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using WebShop.Api.Controllers;
using WebShop.Api.Models;
using WebShop.Business.DTOs;
using WebShop.Business.Models;
using WebShop.Business.Services.Interfaces;
using Xunit;

namespace WebShop.Api.Tests.Controllers;

/// <summary>
/// Unit tests for OrderController.
/// </summary>
[Trait("Category", TestCategories.Unit)]
public class OrderControllerTests
{
    private readonly Mock<IOrderService> mockService = new();
    private readonly Mock<ILogger<OrderController>> mockLogger = new();
    private readonly OrderController controller;

    public OrderControllerTests()
    {
        controller = new OrderController(mockService.Object, mockLogger.Object);
    }

    #region GetAll Tests

    [Fact]
    public async Task GetAll_NonPaginated_ReturnsOkWithOrders()
    {
        // Arrange
        List<OrderDto> orders =
        [
            new() { Id = 1, CustomerId = 1 },
            new() { Id = 2, CustomerId = 2 }
        ];
        mockService.Setup(s => s.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(orders);

        // Act
        IActionResult result = await controller.GetAll(new PaginationQuery(), CancellationToken.None);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result as OkObjectResult;
        Response<IReadOnlyList<OrderDto>>? response = okResult!.Value as Response<IReadOnlyList<OrderDto>>;
        response!.Data.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAll_Paginated_ReturnsPagedResult()
    {
        // Arrange
        List<OrderDto> orders = [new() { Id = 1, CustomerId = 1 }];
        mockService.Setup(s => s.GetPagedAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync((orders, 100));

        // Act
        IActionResult result = await controller.GetAll(new PaginationQuery { Page = 1, PageSize = 20 }, CancellationToken.None);

        // Assert - OkResponse wraps PagedResult in Response<T>
        result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result as OkObjectResult;
        Response<PagedResult<OrderDto>>? response = okResult!.Value as Response<PagedResult<OrderDto>>;
        PagedResult<OrderDto>? pagedResult = response!.Data;
        pagedResult!.Items.Should().HaveCount(1);
        pagedResult.TotalCount.Should().Be(100);
    }

    #endregion

    #region GetById Tests

    [Fact]
    public async Task GetById_ValidId_ReturnsOkWithOrder()
    {
        // Arrange
        const int orderId = 1;
        OrderDto order = new() { Id = orderId, CustomerId = 1 };
        mockService.Setup(s => s.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<OrderDto>.Success(order));

        // Act
        ActionResult<Response<OrderDto>> result = await controller.GetById(orderId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<OrderDto>? response = okResult!.Value as Response<OrderDto>;
        response!.Data!.Id.Should().Be(orderId);
    }

    [Fact]
    public async Task GetById_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int orderId = 999;
        mockService.Setup(s => s.GetByIdAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(Result<OrderDto>.NotFound());

        // Act
        ActionResult<Response<OrderDto>> result = await controller.GetById(orderId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region GetByCustomerId Tests

    [Fact]
    public async Task GetByCustomerId_ValidCustomerId_ReturnsOkWithOrders()
    {
        // Arrange
        const int customerId = 1;
        List<OrderDto> orders = [new() { Id = 1, CustomerId = customerId }];
        mockService.Setup(s => s.GetByCustomerIdAsync(customerId, It.IsAny<CancellationToken>())).ReturnsAsync(orders);

        // Act
        ActionResult<Response<IReadOnlyList<OrderDto>>> result = await controller.GetByCustomerId(customerId, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<OrderDto>>? response = okResult!.Value as Response<IReadOnlyList<OrderDto>>;
        response!.Data.Should().HaveCount(1);
        response.Data![0].CustomerId.Should().Be(customerId);
    }

    #endregion

    #region GetByDateRange Tests

    [Fact]
    public async Task GetByDateRange_ValidRange_ReturnsOkWithOrders()
    {
        // Arrange
        DateTime startDate = new(2024, 1, 1);
        DateTime endDate = new(2024, 12, 31);
        List<OrderDto> orders = [new() { Id = 1, CustomerId = 1 }];
        mockService.Setup(s => s.GetByDateRangeAsync(startDate, endDate, It.IsAny<CancellationToken>())).ReturnsAsync(orders);

        // Act
        ActionResult<Response<IReadOnlyList<OrderDto>>> result = await controller.GetByDateRange(startDate, endDate, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<OkObjectResult>();
        OkObjectResult? okResult = result.Result as OkObjectResult;
        Response<IReadOnlyList<OrderDto>>? response = okResult!.Value as Response<IReadOnlyList<OrderDto>>;
        response!.Data.Should().HaveCount(1);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ValidDto_ReturnsCreated()
    {
        // Arrange
        CreateOrderDto createDto = new() { CustomerId = 1, ShippingAddressId = 1 };
        OrderDto created = new() { Id = 1, CustomerId = 1, ShippingAddressId = 1 };
        mockService.Setup(s => s.CreateAsync(createDto, It.IsAny<CancellationToken>())).ReturnsAsync(created);

        // Act
        ActionResult<Response<OrderDto>> result = await controller.Create(createDto, CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<CreatedAtActionResult>();
        CreatedAtActionResult? createdResult = result.Result as CreatedAtActionResult;
        Response<OrderDto>? response = createdResult!.Value as Response<OrderDto>;
        response!.Data!.Id.Should().Be(1);
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int orderId = 1;
        UpdateOrderDto updateDto = new();
        mockService.Setup(s => s.UpdateAsync(orderId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<OrderDto>.Success(new OrderDto { Id = orderId }));

        // Act
        IActionResult result = await controller.Update(orderId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Update_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int orderId = 999;
        UpdateOrderDto updateDto = new();
        mockService.Setup(s => s.UpdateAsync(orderId, updateDto, It.IsAny<CancellationToken>())).ReturnsAsync(Result<OrderDto>.NotFound());

        // Act
        IActionResult result = await controller.Update(orderId, updateDto, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ValidId_ReturnsNoContent()
    {
        // Arrange
        const int orderId = 1;
        mockService.Setup(s => s.DeleteAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        // Act
        IActionResult result = await controller.Delete(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task Delete_InvalidId_ReturnsNotFound()
    {
        // Arrange
        const int orderId = 999;
        mockService.Setup(s => s.DeleteAsync(orderId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        // Act
        IActionResult result = await controller.Delete(orderId, CancellationToken.None);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    #endregion
}
